using System.IO;
using System.Text.RegularExpressions;

namespace RottrModManager.Shared.Cdc
{
    public static class ResourceNaming
    {
        public static ResourceType GetType(string filePath)
        {
            return Path.GetExtension(filePath) switch
                   {
                       ".anim" => ResourceType.Animation,
                       ".dtp" => ResourceType.Dtp,
                       ".grplist" => ResourceType.GroupList,
                       ".material" => ResourceType.Material,
                       ".object" => ResourceType.Object,
                       ".psdres" => ResourceType.PsdRes,
                       ".script" => ResourceType.Script,
                       ".shader" => ResourceType.Shader,
                       ".skl" => ResourceType.Dtp,
                       ".sound" => ResourceType.Sound,
                       ".tr2cmesh" => ResourceType.CollisionMesh,
                       ".tr2mesh" => ResourceType.Mesh,
                       ".tr2pcd" => ResourceType.Texture,
                       ".trigger" => ResourceType.Trigger,
                       ".unused" => ResourceType.Unused,
                       _ => ResourceType.Unknown
                   };
        }

        public static string GetExtension(ResourceType type)
        {
            return type switch
                   {
                       ResourceType.Animation => ".anim",
                       ResourceType.Dtp => ".dtp",
                       ResourceType.GroupList => ".grplist",
                       ResourceType.Material => ".material",
                       ResourceType.Object => ".object",
                       ResourceType.PsdRes => ".psdres",
                       ResourceType.Script => ".script",
                       ResourceType.Shader => ".shader",
                       ResourceType.Sound => ".sound",
                       ResourceType.CollisionMesh => ".tr2cmesh",
                       ResourceType.Mesh => ".tr2mesh",
                       ResourceType.Texture => ".tr2pcd",
                       ResourceType.Trigger => ".trigger",
                       ResourceType.Unused => ".unused",
                       _ => ".type" + (int)type
                   };
        }

        public static string GetName(Stream stream, ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Material:
                    return Sanitize(new CdcMaterial(stream).Name);
                
                case ResourceType.Mesh:
                    return Sanitize(new CdcMesh(stream).Name);

                case ResourceType.Sound:
                    return Sanitize(new CdcSound(stream).Name);

                case ResourceType.Texture:
                    return Sanitize(new CdcTexture(stream).Name);

                default:
                    return null;
            }
        }

        public static string GetName(ArchiveSet archiveSet, ResourceReference resourceRef)
        {
            switch (resourceRef.Type)
            {
                case ResourceType.Material:
                case ResourceType.Mesh:
                case ResourceType.Sound:
                case ResourceType.Texture:
                {
                    using Stream stream = archiveSet.OpenResource(resourceRef);
                    return GetName(stream, resourceRef.Type);
                }

                default:
                    return null;
            }
        }

        public static string GetFilePath(ArchiveSet archiveSet, ResourceReference resourceRef, int resourceIdx)
        {
            string name = GetName(archiveSet, resourceRef) ?? $"Section {resourceIdx:d04}";
            string extension = GetExtension(resourceRef.Type);
            if (resourceRef.Type == ResourceType.Dtp)
            {
                using Stream stream = archiveSet.OpenResource(resourceRef);
                if (stream.Length > 4)
                {
                    BinaryReader reader = new BinaryReader(stream);
                    if (reader.ReadInt32() == 6)
                        extension = ".skl";
                }
            }

            return name + extension;
        }

        private static string Sanitize(string name)
        {
            if (name == null)
                return null;

            return Regex.Replace(name, @"[^- \.\w\\]", "_");
        }
    }
}
