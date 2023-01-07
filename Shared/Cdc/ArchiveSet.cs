using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using RottrModManager.Shared.Util;

namespace RottrModManager.Shared.Cdc
{
    public class ArchiveSet : IDisposable
    {
        private readonly Dictionary<int, Archive> _archives = new Dictionary<int, Archive>();
        private readonly Dictionary<ArchiveFileIdentifier, ArchiveFileReference> _files = new Dictionary<ArchiveFileIdentifier, ArchiveFileReference>();

        public ArchiveSet(string folderPath)
        {
            FolderPath = folderPath;

            foreach (string nfoFilePath in Directory.EnumerateFiles(folderPath))
            {
                if (!nfoFilePath.EndsWith(".nfo") && !nfoFilePath.EndsWith(".nfo.disabled"))
                    continue;

                string archiveFilePath = Regex.Replace(nfoFilePath, @"\.nfo(\.disabled)?$", ".tiger");
                if (!File.Exists(archiveFilePath))
                    continue;

                Archive archive = Archive.Open(archiveFilePath);
                _archives.Add(archive.Id, archive);
            }

            foreach (Archive archive in GetSortedArchives())
            {
                foreach (ArchiveFileReference file in archive.Files)
                {
                    _files[file] = file;
                }
            }
        }

        public string FolderPath
        {
            get;
        }

        public IReadOnlyCollection<Archive> Archives => _archives.Values;

        public Archive GetArchive(int id)
        {
            return _archives.GetOrDefault(id);
        }

        public IReadOnlyCollection<ArchiveFileReference> Files => _files.Values;

        public Archive CreateModArchive(string modName, int maxResourceCollections)
        {
            string simplifiedName = Regex.Replace(modName, @"[^-.\w]", "_").ToLower();
            simplifiedName = Regex.Replace(simplifiedName, @"__+", "_").Trim('_');

            int gameId = _archives.Values.First().MetaData.GameId;
            int version = _archives.Values.Max(a => a.MetaData.Version) + 1;
            int id = _archives.Values.Max(a => a.Id) + 1;
            
            string filePath = Path.Combine(FolderPath, $"bigfile._mod.{simplifiedName}.000.000.tiger");
            Archive archive = Archive.Create(filePath, gameId, version, id, maxResourceCollections);

            string steamId = _archives.Values.First().MetaData.CustomEntries.FirstOrDefault(c => c.StartsWith("steamID:"));
            if (steamId != null)
                archive.MetaData.CustomEntries.Add(steamId);

            archive.MetaData.CustomEntries.Add("mod:" + modName);
            archive.MetaData.Save();
            return archive;
        }

        public void Add(Archive archive, ITaskProgress progress, CancellationToken cancellationToken)
        {
            _archives.Add(archive.Id, archive);
            List<Archive> sortedArchives = GetSortedArchives();
            int index = sortedArchives.IndexOf(archive);
            if (index >= 0)
                UpdateResourceReferences(sortedArchives, index + 1, progress, cancellationToken);
        }

        public void Enable(Archive archive, ITaskProgress progress, CancellationToken cancellationToken)
        {
            try
            {
                progress.Begin($"Enabling mod {archive.ModName}...");

                archive.Enabled = true;

                List<Archive> sortedArchives = GetSortedArchives();
                int index = sortedArchives.IndexOf(archive);
                if (index >= 0)
                    UpdateResourceReferences(sortedArchives, index + 1, progress, cancellationToken);
            }
            finally
            {
                progress.End();
            }
        }

        public void Disable(Archive archive, ITaskProgress progress, CancellationToken cancellationToken)
        {
            Disable(archive, progress, $"Disabling mod {archive.ModName}...", cancellationToken);
        }

        private void Disable(Archive archive, ITaskProgress progress, string statusText, CancellationToken cancellationToken)
        {
            try
            {
                progress.Begin(statusText);

                List<Archive> sortedArchives = GetSortedArchives();
                int index = sortedArchives.IndexOf(archive);
                archive.Enabled = false;
                if (index >= 0)
                {
                    sortedArchives.RemoveAt(index);
                    UpdateResourceReferences(sortedArchives, index, progress, cancellationToken);
                }
                else
                {
                    UpdateResourceReferences(sortedArchives, sortedArchives.Count, progress, cancellationToken);
                }
            }
            finally
            {
                progress.End();
            }
        }

        public void Delete(Archive archive, ITaskProgress progress, CancellationToken cancellationToken)
        {
            Disable(archive, progress, $"Removing mod {archive.ModName}...", cancellationToken);
            archive.Delete();
            archive.Dispose();
            _archives.Remove(archive.Id);
        }

        private void UpdateResourceReferences(List<Archive> sortedArchives, int startIndex, ITaskProgress progress, CancellationToken cancellationToken)
        {
            _files.Clear();

            for (int i = 0; i < startIndex; i++)
            {
                foreach (ArchiveFileReference file in sortedArchives[i].Files)
                {
                    _files[file] = file;
                }
            }

            int numTotalFiles = 0;
            for (int i = startIndex; i < sortedArchives.Count; i++)
            {
                numTotalFiles += sortedArchives[i].Files.Count;
            }

            int numUpdatedFiles = 0;
            for (int i = startIndex; i < sortedArchives.Count; i++)
            {
                foreach (ArchiveFileReference file in sortedArchives[i].Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    numUpdatedFiles++;
                    progress.Report((float)numUpdatedFiles / numTotalFiles);

                    ArchiveFileReference prevFile = _files.GetOrDefault(file);
                    if (prevFile == null)
                    {
                        _files[file] = file;
                        continue;
                    }

                    ResourceCollection collection = GetResourceCollection(file);
                    ResourceCollection prevCollection = GetResourceCollection(prevFile);
                    if (collection == null || prevCollection == null)
                        continue;

                    for (int j = 0; j < collection.ResourceReferences.Count; j++)
                    {
                        if (collection.ResourceReferences[j].ArchiveId != file.ArchiveId)
                            collection.UpdateResourceReference(j, prevCollection.ResourceReferences[j]);
                    }

                    _files[file] = file;
                }
            }
        }

        public ArchiveFileReference GetFileReference(ArchiveFileIdentifier fileId)
        {
            return GetFileReference(fileId.NameHash, fileId.Locale);
        }

        public ArchiveFileReference GetFileReference(uint nameHash, int locale = -1)
        {
            _files.TryGetValue(new ArchiveFileIdentifier(nameHash, locale), out ArchiveFileReference file);
            return file;
        }

        public ResourceCollection GetResourceCollection(ArchiveFileReference file)
        {
            return _archives[file.ArchiveId].GetResourceCollection(file);
        }

        public Stream OpenResource(ResourceReference resourceRef)
        {
            return _archives[resourceRef.ArchiveId].OpenResource(resourceRef);
        }

        public byte[] GetBlob(ArchiveBlobReference blobRef)
        {
            return _archives[blobRef.ArchiveId].GetBlob(blobRef);
        }

        private List<Archive> GetSortedArchives()
        {
            List<Archive> sortedArchives = _archives.Values.Where(a => a.Enabled).ToList();
            sortedArchives.Sort(CompareArchivePriority);
            return sortedArchives;
        }

        private static int CompareArchivePriority(Archive a, Archive b)
        {
            int comparison = a.MetaData.Version.CompareTo(b.MetaData.Version);
            if (comparison != 0)
                return comparison;

            comparison = b.MetaData.Required.CompareTo(a.MetaData.Required);
            if (comparison != 0)
                return comparison;

            comparison = a.MetaData.PackageId.CompareTo(b.MetaData.PackageId);
            if (comparison != 0)
                return comparison;

            return a.MetaData.Chunk.CompareTo(b.MetaData.Chunk);
        }

        public void CloseStreams()
        {
            foreach (Archive archive in _archives.Values)
            {
                archive.CloseStreams();
            }
        }

        public void Dispose()
        {
            foreach (Archive archive in _archives.Values)
            {
                archive.Dispose();
            }
            _archives.Clear();
        }
    }
}
