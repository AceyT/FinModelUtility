﻿using fin.io.bundles;

namespace uni.ui.common.fileTreeView {
  public interface IFileTreeView {
    public delegate void FileSelectedHandler(IFileTreeLeafNode fileNode);

    event FileSelectedHandler FileSelected;


    public delegate void DirectorySelectedHandler(
        IFileTreeParentNode directoryNode);

    event DirectorySelectedHandler DirectorySelected;

    Image GetImageForFile(IFileBundle file);
  }

  public interface IFileTreeNode {
    string Text { get; }
    IFileTreeParentNode? Parent { get; }
  }

  public interface IFileTreeParentNode : IFileTreeNode {
    IEnumerable<IFileTreeNode> ChildNodes { get; }

    IEnumerable<IFileBundle> GetFiles(bool recursive);

    IEnumerable<TSpecificFile> GetFilesOfType<TSpecificFile>(bool recursive)
        where TSpecificFile : IFileBundle;
  }

  public interface IFileTreeLeafNode : IFileTreeNode {
    IFileBundle File { get; }
  }
}