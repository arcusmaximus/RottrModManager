﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using RottrModManager.Shared.Util;
using VirtualTreeView;

namespace RottrExtractor.Controls
{
    internal class FileTreeView<TFile> : FileTreeViewBase
        where TFile : class
    {
        private FileTreeNode _rootFileNode;
        private int _searchCounter;
        private string _filter;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _txtSearch.TextChanged += _txtSearch_TextChanged;

            _tvFiles.Header.Columns.Add(new VirtualTreeColumn { Name = "" });
            _tvFiles.OnGetNodeCellImage += OnGetNodeCellCellImage;
            _tvFiles.OnGetNodeCellText += GetCellText;
            _tvFiles.OnSelectionChanged += (s, e) => SelectionChanged?.Invoke(this, EventArgs.Empty);

            OnResize(e);
        }

        public void Clear()
        {
            _tvFiles.Clear();
        }

        protected void Populate(FileTreeNode rootFileNode)
        {
            _rootFileNode = rootFileNode;
            CreateTreeNodes();
        }

        public event EventHandler SelectionChanged;

        public TFile ActiveFile
        {
            get
            {
                if (_tvFiles.ActiveNode == null)
                    return null;

                FileTreeNode fileNode = _tvFiles.GetNodeData<FileTreeNode>(_tvFiles.ActiveNode);
                return fileNode?.File;
            }
        }

        public List<TFile> SelectedFiles
        {
            get
            {
                List<TFile> files = new List<TFile>();
                foreach (VirtualTreeNode treeNode in _tvFiles.SelectedNodes)
                {
                    FileTreeNode fileNode = _tvFiles.GetNodeData<FileTreeNode>(treeNode);
                    GetFilesRecursive(fileNode, files);
                }
                return files;
            }
        }

        private static void GetFilesRecursive(FileTreeNode fileNode, List<TFile> files)
        {
            if (fileNode.File != null)
                files.Add(fileNode.File);

            foreach (FileTreeNode childNode in fileNode.Children)
            {
                GetFilesRecursive(childNode, files);
            }
        }

        protected virtual void CreateTreeNodes()
        {
            _tvFiles.BeginUpdate();
            _tvFiles.Clear();
            CreateTreeNodes(_rootFileNode, null);
            _tvFiles.EndUpdate();
        }

        private void CreateTreeNodes(FileTreeNode parentFileNode, VirtualTreeNode parentTreeNode)
        {
            foreach (FileTreeNode childFileNode in parentFileNode.Children)
            {
                if (_filter != null && childFileNode.Type == FileTreeNodeType.File && !childFileNode.Name.Contains(_filter))
                    continue;

                VirtualTreeNode childTreeNode = _tvFiles.InsertNode(parentTreeNode, NodeAttachMode.amAddChildLast, childFileNode);
                childTreeNode.nodeHeight += 2;
                CreateTreeNodes(childFileNode, childTreeNode);

                if (_filter != null && childFileNode.Type == FileTreeNodeType.Folder && childTreeNode.childCount == 0)
                    _tvFiles.DeleteNode(childTreeNode);
            }
        }

        private async void _txtSearch_TextChanged(object sender, EventArgs e)
        {
            int searchCounter = ++_searchCounter;
            string filter = _txtSearch.Text;

            await Task.Delay(1000);
            if (searchCounter != _searchCounter)
                return;

            _filter = filter;
            CreateTreeNodes();
        }

        private void OnGetNodeCellCellImage(VirtualTreeView.VirtualTreeView tree, VirtualTreeNode treeNode, int column, out Image image)
        {
            FileTreeNode fileNode = _tvFiles.GetNodeData<FileTreeNode>(treeNode);
            image = fileNode?.Image;
        }

        private void GetCellText(VirtualTreeView.VirtualTreeView tree, VirtualTreeNode treeNode, int column, out string celltext)
        {
            FileTreeNode fileNode = _tvFiles.GetNodeData<FileTreeNode>(treeNode);
            if (fileNode == null)
            {
                celltext = string.Empty;
                return;
            }

            celltext = column switch
            {
                0 => fileNode.Name,
                _ => string.Empty
            };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_tvFiles.Header.Columns.Count == 0)
                return;

            _tvFiles.BeginUpdate();
            _tvFiles.Header.Columns[0].Width = Width - 20;
            _tvFiles.EndUpdate();
        }

        protected class FileTreeNode
        {
            private readonly SortedList<(FileTreeNodeType, string), FileTreeNode> _children = new SortedList<(FileTreeNodeType, string), FileTreeNode>();

            public FileTreeNode(string name)
            {
                Name = name;
            }

            public string Name
            {
                get;
            }

            public FileTreeNodeType Type
            {
                get;
                set;
            }

            public TFile File
            {
                get;
                set;
            }

            public Image Image
            {
                get;
                set;
            }

            public IList<FileTreeNode> Children => _children.Values;

            public void Add(FileTreeNode node)
            {
                _children.Add((node.Type, node.Name), node);
            }

            public FileTreeNode Add(string path)
            {
                return Add(path.Split('\\'), 0);
            }

            public FileTreeNode Add(string[] parts, int index)
            {
                string name = parts[index];
                FileTreeNodeType type = index == parts.Length - 1 ? FileTreeNodeType.File : FileTreeNodeType.Folder;
                FileTreeNode child = _children.GetOrAdd(
                    (type, name),
                    () => new FileTreeNode(name)
                          {
                              Type = type,
                              Image = type == FileTreeNodeType.Folder ? FolderImage : FileImage
                          }
                );
                if (index < parts.Length - 1)
                    return child.Add(parts, index + 1);

                return child;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
