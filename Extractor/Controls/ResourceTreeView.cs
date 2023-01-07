using System.Collections.Generic;
using System.Drawing;
using RottrModManager.Shared.Cdc;
using RottrModManager.Shared.Util;

namespace RottrExtractor.Controls
{
    internal class ResourceTreeViewBase : FileTreeView<ResourceReference>
    {
    }

    internal class ResourceTreeView : ResourceTreeViewBase
    {
        private static readonly Dictionary<ResourceType, Image> TypeImages =
            new Dictionary<ResourceType, Image>
            {
                { ResourceType.Animation, Properties.Resources.Animation },
                { ResourceType.Material, Properties.Resources.Material },
                { ResourceType.Mesh, Properties.Resources.Mesh },
                { ResourceType.Script, Properties.Resources.Script },
                { ResourceType.Shader, Properties.Resources.Shader },
                { ResourceType.Sound, Properties.Resources.Sound },
                { ResourceType.Texture, Properties.Resources.Texture },
            };

        public void Populate(ArchiveSet archiveSet, ResourceCollection collection)
        {
            FileTreeNode rootNode = new FileTreeNode(null);

            for (int i = 0; i < collection.ResourceReferences.Count; i++)
            {
                ResourceReference resourceRef = collection.ResourceReferences[i];
                string filePath = resourceRef.Type + "\\" + ResourceNaming.GetFilePath(archiveSet, resourceRef, i);
                FileTreeNode fileNode = rootNode.Add(filePath);
                fileNode.File = resourceRef;
                fileNode.Image = TypeImages.GetOrDefault(resourceRef.Type) ?? Properties.Resources.File;
            }
            archiveSet.CloseStreams();

            Populate(rootNode);
        }
    }
}
