using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RottrModManager.Shared.Cdc;

namespace RottrModManager.Mod
{
    internal class FolderModPackage : ModPackage
    {
        private readonly string _folderPath;
        private readonly Dictionary<ArchiveFileIdentifier, string> _filePaths = new Dictionary<ArchiveFileIdentifier, string>();

        public FolderModPackage(string folderPath)
        {
            _folderPath = folderPath;
            if (!_folderPath.EndsWith("\\"))
                _folderPath += "\\";

            foreach (string filePath in Directory.EnumerateFiles(_folderPath, "*", SearchOption.AllDirectories))
            {
                uint hash;
                int locale;
                Match match = Regex.Match(Path.GetFileNameWithoutExtension(filePath), @"^Locale ([\dA-F]+)");
                if (match.Success)
                {
                    hash = CdcHash.Calculate(Path.GetDirectoryName(filePath).Substring(_folderPath.Length));
                    locale = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
                }
                else
                {
                    hash = CdcHash.Calculate(filePath.Substring(_folderPath.Length));
                    locale = -1;
                }
                _filePaths[new ArchiveFileIdentifier(hash, locale)] = filePath;
            }
        }

        public override IEnumerable<ArchiveFileIdentifier> FileIdentifiers => _filePaths.Keys;

        public override byte[] GetFileContent(ArchiveFileIdentifier fileId)
        {
            return File.ReadAllBytes(_filePaths[fileId]);
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

            string folderPath = resourceFilePath;
            string collectionFilePath;
            do
            {
                folderPath = Path.GetDirectoryName(folderPath);
                collectionFilePath = Directory.EnumerateFiles(folderPath, "*.drm").FirstOrDefault();
                if (collectionFilePath != null)
                    break;
            } while (folderPath.Trim('\\') != _folderPath.Trim('\\'));
            
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
