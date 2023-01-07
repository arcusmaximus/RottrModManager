using System.Collections.Generic;
using System.IO;
using System.Threading;
using RottrModManager.Shared.Util;

namespace RottrModManager.Shared.Cdc
{
    public class ResourceUsageCache
    {
        private const string FileName = "resourceusage.bin";
        private const int Version = 1;

        private readonly ArchiveSet _archiveSet;
        private readonly Dictionary<string, List<ResourceUsage>> _usages = new Dictionary<string, List<ResourceUsage>>();

        public ResourceUsageCache(ArchiveSet archiveSet)
        {
            _archiveSet = archiveSet;
        }

        public void Refresh(ITaskProgress progress, CancellationToken cancellationToken)
        {
            try
            {
                progress.Begin("Creating resource usage cache...");

                _usages.Clear();

                int collectionIdx = 0;
                foreach (ArchiveFileReference collectionRef in _archiveSet.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ResourceCollection collection = _archiveSet.GetResourceCollection(collectionRef);
                    if (collection == null)
                    {
                        collectionIdx++;
                        continue;
                    }

                    for (int resourceIdx = 0; resourceIdx < collection.ResourceReferences.Count; resourceIdx++)
                    {
                        string name = ResourceNaming.GetName(_archiveSet, collection.ResourceReferences[resourceIdx]);
                        if (name != null)
                        {
                            _usages.GetOrAdd(name, () => new List<ResourceUsage>())
                                   .Add(new ResourceUsage(collectionRef.NameHash, resourceIdx));
                        }
                    }

                    collectionIdx++;
                    progress.Report((float)collectionIdx / _archiveSet.Files.Count);
                }
            }
            finally
            {
                progress.End();
            }
        }

        public IEnumerable<ResourceCollectionItemReference> GetUsages(string resourceName)
        {
            List<ResourceUsage> usages = _usages.GetOrDefault(resourceName);
            if (usages == null)
                yield break;

            foreach (ResourceUsage usage in usages)
            {
                yield return new ResourceCollectionItemReference(_archiveSet.GetFileReference(usage.CollectionNameHash), usage.ResourceIndex);
            }
        }

        public bool Load()
        {
            string filePath = Path.Combine(_archiveSet.FolderPath, FileName);
            if (!File.Exists(filePath))
                return false;

            using Stream stream = File.OpenRead(filePath);
            BinaryReader reader = new BinaryReader(stream);
            int version = reader.ReadInt32();
            if (version != Version)
                return false;

            int archiveDateHash = reader.ReadInt32();
            if (archiveDateHash != GetArchiveDateHash())
                return false;

            int numResources = reader.ReadInt32();
            for (int i = 0; i < numResources; i++)
            {
                string resourceName = reader.ReadString();
                int numUsages = reader.ReadInt32();
                List<ResourceUsage> usages = new List<ResourceUsage>(numUsages);
                for (int j = 0; j < numUsages; j++)
                {
                    uint collectionNameHash = reader.ReadUInt32();
                    int resourceIdx = reader.ReadInt32();
                    usages.Add(new ResourceUsage(collectionNameHash, resourceIdx));
                }
                _usages.Add(resourceName, usages);
            }
            return true;
        }

        public void Save()
        {
            using Stream stream = File.Create(Path.Combine(_archiveSet.FolderPath, FileName));
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(Version);
            writer.Write(GetArchiveDateHash());

            writer.Write(_usages.Count);
            foreach ((string resourceName, List<ResourceUsage> usages) in _usages)
            {
                writer.Write(resourceName);
                writer.Write(usages.Count);
                foreach (ResourceUsage usage in usages)
                {
                    writer.Write(usage.CollectionNameHash);
                    writer.Write(usage.ResourceIndex);
                }
            }
        }

        private int GetArchiveDateHash()
        {
            int hash = 0;
            unchecked
            {
                foreach (string archiveFilePath in Directory.EnumerateFiles(_archiveSet.FolderPath, "*.tiger"))
                {
                    if (!Path.GetFileName(archiveFilePath).StartsWith("bigfile._mod"))
                        hash = (hash * 397) ^ File.GetLastWriteTime(archiveFilePath).GetHashCode();
                }
            }
            return hash;
        }

        private struct ResourceUsage
        {
            public ResourceUsage(uint collectionNameHash, int resourceIndex)
            {
                CollectionNameHash = collectionNameHash;
                ResourceIndex = resourceIndex;
            }

            public uint CollectionNameHash
            {
                get;
            }

            public int ResourceIndex
            {
                get;
            }
        }
    }
}
