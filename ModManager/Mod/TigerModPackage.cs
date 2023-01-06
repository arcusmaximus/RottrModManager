using System.Collections.Generic;
using System.IO;
using System.Linq;
using RottrModManager.Shared.Cdc;
using RottrModManager.Shared.Util;
using RottrModManager.Util;

namespace RottrModManager.Mod
{
    internal class TigerModPackage : ModPackage
    {
        private readonly Archive _archive;
        private readonly Dictionary<int, ResourceInfo> _resources = new Dictionary<int, ResourceInfo>();

        public TigerModPackage(string filePath)
        {
            _archive = Archive.Open(filePath);

            foreach (ResourceCollectionReference collectionRef in _archive.ResourceCollections)
            {
                ResourceCollection collection = _archive.GetResourceCollection(collectionRef);
                for (int resourceIdx = 0; resourceIdx < collection.ResourceReferences.Count; resourceIdx++)
                {
                    ResourceReference resourceRef = collection.ResourceReferences[resourceIdx];
                    if (resourceRef.ArchiveId != _archive.Id)
                        continue;

                    ResourceInfo resourceInfo = _resources.GetOrAdd(resourceRef.Offset, () => new ResourceInfo(resourceRef));
                    resourceInfo.Usages.Add(new ResourceCollectionItemReference(collectionRef, resourceIdx));
                }
            }
        }

        public override IEnumerable<string> ResourceKeys
        {
            get
            {
                return _resources.Keys.Select(k => k.ToString());
            }
        }

        public override ResourceType GetResourceType(string key)
        {
            return _resources[int.Parse(key)].Reference.Type;
        }

        public override IEnumerable<(ResourceCollection, int)> GetResourceUsages(string key)
        {
            ResourceInfo resourceInfo = _resources[int.Parse(key)];
            foreach (ResourceCollectionItemReference usage in resourceInfo.Usages)
            {
                yield return (_archive.GetResourceCollection(usage.Collection), usage.ResourceIndex);
            }
        }

        public override Stream OpenResource(string key)
        {
            ResourceReference resourceRef = _resources[int.Parse(key)].Reference;
            return _archive.OpenResource(resourceRef);
        }

        public override void Dispose()
        {
            base.Dispose();
            _archive.Dispose();
        }

        private class ResourceInfo
        {
            public ResourceInfo(ResourceReference resourceRef)
            {
                Reference = resourceRef;
            }

            public ResourceReference Reference
            {
                get;
            }

            public List<ResourceCollectionItemReference> Usages
            {
                get;
            } = new List<ResourceCollectionItemReference>();

        }
    }
}
