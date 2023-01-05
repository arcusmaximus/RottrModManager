using System.IO;

namespace RottrModManager.Cdc
{
    internal static class ResourceParser
    {
        public static ResourceType GetType(string filePath)
        {
            return Path.GetExtension(filePath) switch
                   {
                       ".anim" => ResourceType.Animation,
                       ".dtp" => ResourceType.Dtp,
                       ".sound" => ResourceType.Sound,
                       ".tr2mesh" => ResourceType.Mesh,
                       ".tr2pcd" => ResourceType.Texture,
                       _ => ResourceType.Unknown
                   };
        }

        public static string GetName(Stream stream, ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Mesh:
                    return new CdcMesh(stream).Name;

                case ResourceType.Texture:
                    return new CdcTexture(stream).Name;

                default:
                    return null;
            }
        }

        public static string GetName(ArchiveSet archiveSet, ResourceReference resourceRef)
        {
            switch (resourceRef.Type)
            {
                case ResourceType.Mesh:
                case ResourceType.Texture:
                {
                    using Stream stream = archiveSet.GetResourceReadStream(resourceRef);
                    return GetName(stream, resourceRef.Type);
                }

                default:
                    return null;
            }
        }
    }
}
