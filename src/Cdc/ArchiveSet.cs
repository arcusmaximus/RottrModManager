using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using RottrModManager.Util;

namespace RottrModManager.Cdc
{
    internal class ArchiveSet : IDisposable
    {
        private readonly Dictionary<int, Archive> _archives = new Dictionary<int, Archive>();
        private readonly Dictionary<uint, ResourceCollectionReference> _resourceCollections = new Dictionary<uint, ResourceCollectionReference>();

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
                foreach (ResourceCollectionReference collection in archive.ResourceCollections)
                {
                    _resourceCollections[collection.NameHash] = collection;
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

        public IReadOnlyCollection<ResourceCollectionReference> ResourceCollections => _resourceCollections.Values;

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
            _resourceCollections.Clear();

            for (int i = 0; i < startIndex; i++)
            {
                foreach (ResourceCollectionReference collectionRef in sortedArchives[i].ResourceCollections)
                {
                    _resourceCollections[collectionRef.NameHash] = collectionRef;
                }
            }

            int totalCollections = 0;
            for (int i = startIndex; i < sortedArchives.Count; i++)
            {
                totalCollections += sortedArchives[i].ResourceCollections.Count;
            }

            int updatedCollections = 0;
            for (int i = startIndex; i < sortedArchives.Count; i++)
            {
                foreach (ResourceCollectionReference collectionRef in sortedArchives[i].ResourceCollections)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    updatedCollections++;
                    progress.Report((float)updatedCollections / totalCollections);

                    ResourceCollectionReference prevCollectionRef = _resourceCollections.GetOrDefault(collectionRef.NameHash);
                    if (prevCollectionRef == null)
                    {
                        _resourceCollections[collectionRef.NameHash] = collectionRef;
                        continue;
                    }

                    ResourceCollection collection = GetResourceCollection(collectionRef);
                    ResourceCollection prevCollection = GetResourceCollection(prevCollectionRef);
                    for (int j = 0; j < collection.ResourceReferences.Count; j++)
                    {
                        if (collection.ResourceReferences[j].ArchiveId != collectionRef.ArchiveId)
                            collection.UpdateResourceReference(j, prevCollection.ResourceReferences[j]);
                    }

                    _resourceCollections[collectionRef.NameHash] = collectionRef;
                }
            }
        }

        public ResourceCollectionReference GetResourceCollectionReference(uint nameHash)
        {
            _resourceCollections.TryGetValue(nameHash, out ResourceCollectionReference collection);
            return collection;
        }

        public ResourceCollection GetResourceCollection(uint nameHash)
        {
            ResourceCollectionReference collectionRef = GetResourceCollectionReference(nameHash);
            return collectionRef != null ? GetResourceCollection(collectionRef) : null;
        }

        public ResourceCollection GetResourceCollection(ResourceCollectionReference collectionRef)
        {
            return _archives[collectionRef.ArchiveId].GetResourceCollection(collectionRef);
        }

        public Stream GetResourceReadStream(ResourceReference resourceRef)
        {
            return _archives[resourceRef.ArchiveId].OpenResource(resourceRef);
        }

        public byte[] GetBytes(ArchiveItemReference itemRef)
        {
            return _archives[itemRef.ArchiveId].GetBytes(itemRef);
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
