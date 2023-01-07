using System.Collections.Generic;
using System.Drawing;
using System.IO;
using RottrModManager.Shared.Cdc;
using RottrModManager.Shared.Util;

namespace RottrExtractor.Controls
{
    internal class ArchiveFileTreeViewBase : FileTreeView<ArchiveFileReference>
    {
    }

    internal class ArchiveFileTreeView : ArchiveFileTreeViewBase
    {
        private static readonly Dictionary<string, Image> ExtensionImages =
            new Dictionary<string, Image>
            {
                { ".bin", Properties.Resources.Localization },
                { ".drm", Properties.Resources.List },
                { ".mul", Properties.Resources.Sound }
            };

        public void Populate(ArchiveSet archiveSet)
        {
            FileTreeNode rootFileNode = CreateFileNodes(archiveSet);
            Populate(rootFileNode);
        }

        protected override void CreateTreeNodes()
        {
            base.CreateTreeNodes();
            _tvFiles.ExpandNode(_tvFiles.GetFirst());
        }

        private static FileTreeNode CreateFileNodes(ArchiveSet archiveSet)
        {
            FileTreeNode rootNode = new FileTreeNode(null);
            foreach (ArchiveFileReference file in archiveSet.Files)
            {
                FileTreeNode fileNode = rootNode.Add(file.Name);
                fileNode.Image = ExtensionImages.GetOrDefault(Path.GetExtension(file.Name)) ?? Properties.Resources.File;

                if (fileNode.Children.Count == 0)
                {
                    if (fileNode.File == null)
                    {
                        fileNode.File = file;
                    }
                    else
                    {
                        FileTreeNode prevLocaleNode = new FileTreeNode($"Locale {fileNode.File.Locale}")
                                                      {
                                                          File = fileNode.File,
                                                          Type = FileTreeNodeType.Locale,
                                                          Image = fileNode.Image
                                                      };
                        fileNode.Add(prevLocaleNode);

                        FileTreeNode localeNode = new FileTreeNode($"Locale {file.Locale}")
                                                  {
                                                      File = file,
                                                      Type = FileTreeNodeType.Locale,
                                                      Image = fileNode.Image
                                                  };
                        fileNode.Add(localeNode);

                        fileNode.File = null;
                    }
                }
                else
                {
                    FileTreeNode localeNode = new FileTreeNode($"Locale {file.Locale}")
                                              {
                                                  File = file,
                                                  Type = FileTreeNodeType.Locale,
                                                  Image = fileNode.Image
                                              };
                    fileNode.Add(localeNode);
                }
            }
            archiveSet.CloseStreams();
            return rootNode;
        }
    }
}
