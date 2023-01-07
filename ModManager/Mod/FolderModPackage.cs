using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RottrModManager.Shared.Cdc;

namespace RottrModManager.Mod
{
    internal class FolderModPackage : ModPackage
    {
        private readonly string _folderPath;

        public FolderModPackage(string folderPath)
        {
            _folderPath = folderPath;
        }

        public override IEnumerable<string> ResourceKeys
        {
            get
            {
                return Directory.EnumerateFiles(_folderPath, "*", SearchOption.AllDirectories);
            }
        }

        public override ResourceType GetResourceType(string filePath)
        {
            return ResourceNaming.GetType(filePath);
        }

        public override IEnumerable<(ResourceCollection, int)> GetResourceUsages(string resourceFilePath)
        {
            Match match = Regex.Match(Path.GetFileNameWithoutExtension(resourceFilePath), @"^(?:Section|Replace)\s+(\d+)$");
            if (!match.Success)
                yield break;

            string folderPath = Path.GetDirectoryName(resourceFilePath);
            string collectionFilePath = Directory.EnumerateFiles(folderPath, "*.drm").SingleOrDefault();
            if (collectionFilePath == null)
                yield break;

            uint nameHash = CdcHash.Calculate("pcx64-w\\" + Path.GetFileName(collectionFilePath));
            ResourceCollection collection;
            using (Stream stream = File.OpenRead(collectionFilePath))
            {
                collection = new ResourceCollection(nameHash, stream);
                _ = collection.ResourceReferences;
            }

            yield return (collection, int.Parse(match.Groups[1].Value) - 1);
        }

        public override Stream OpenResource(string filePath)
        {
            return File.OpenRead(filePath);
        }
    }
}
