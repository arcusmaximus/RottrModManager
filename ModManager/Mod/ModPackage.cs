using System;
using System.Collections.Generic;
using System.IO;
using RottrModManager.Shared.Cdc;

namespace RottrModManager.Mod
{
    internal abstract class ModPackage : IDisposable
    {
        public abstract IEnumerable<ArchiveFileIdentifier> FileIdentifiers
        {
            get;
        }

        public abstract byte[] GetFileContent(ArchiveFileIdentifier fileId);

        public abstract IEnumerable<string> ResourceKeys
        {
            get;
        }

        public abstract ResourceType GetResourceType(string key);

        public abstract IEnumerable<(ResourceCollection, int)> GetResourceUsages(string key);

        public abstract Stream OpenResource(string key);

        public virtual void Dispose()
        {
        }
    }
}
