﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

using fin.data.stacks;
using fin.util.asserts;
using fin.util.data;

using fins.io.sharpDirLister;

using schema.binary;
using schema.text;

namespace fin.io {
  public class FileHierarchy : IFileHierarchy {
    public FileHierarchy(ISystemDirectory directory) : this(
        directory.Name,
        directory) { }

    public FileHierarchy(string name, ISystemDirectory directory) {
      this.Name = name;
      var populatedSubdirs =
          new SharpFileLister().FindNextFilePInvokeRecursiveParalleled(
              directory.FullPath);
      this.Root = new FileHierarchyDirectory(this,
                                             directory,
                                             populatedSubdirs);
    }

    public string Name { get; }
    public IFileHierarchyDirectory Root { get; }


    private abstract class BFileHierarchyIoObject : IFileHierarchyIoObject {
      protected BFileHierarchyIoObject(IFileHierarchy hierarchy) {
        this.Hierarchy = hierarchy;
        this.LocalPath = string.Empty;
      }

      protected BFileHierarchyIoObject(
          IFileHierarchy hierarchy,
          IFileHierarchyDirectory root,
          IFileHierarchyDirectory parent,
          ISystemIoObject instance) {
        this.Hierarchy = hierarchy;
        this.Parent = parent;

        this.LocalPath =
            instance.FullPath.Substring(root.FullPath.Length);
      }

      protected abstract ISystemIoObject Instance { get; }

      public string LocalPath { get; }
      public IFileHierarchy Hierarchy { get; }
      public IFileHierarchyDirectory? Parent { get; }

      public bool Equals(IReadOnlyTreeIoObject? other)
        => this.Instance.Equals(other);

      public IReadOnlyTreeDirectory AssertGetParent()
        => Asserts.True(this.TryGetParent(out var parent)) ? parent : default!;

      public bool TryGetParent(out IReadOnlyTreeDirectory parent)
        => this.Instance.TryGetParent(out parent);

      public IEnumerable<IReadOnlyTreeDirectory> GetAncestry()
        => this.Instance.GetAncestry();

      public bool Exists => this.Instance.Exists;
      public string FullPath => this.Instance.FullPath;

      public string Name => this.Parent == null
          ? this.Hierarchy.Name
          : this.Instance.Name;

      public override string ToString() => this.LocalPath;
    }


    private class FileHierarchyDirectory : BFileHierarchyIoObject,
                                           IFileHierarchyDirectory {
      private readonly List<IFileHierarchyDirectory> subdirs_ = new();
      private readonly List<IFileHierarchyFile> files_ = new();

      public FileHierarchyDirectory(
          IFileHierarchy hierarchy,
          ISystemDirectory root,
          ISubdirPaths paths) : base(hierarchy) {
        this.Impl = root;

        foreach (var filePath in paths.AbsoluteFilePaths) {
          this.files_.Add(
              new FileHierarchyFile(hierarchy,
                                    this,
                                    this,
                                    new FinFile(filePath)));
        }

        foreach (var subdir in paths.Subdirs) {
          this.subdirs_.Add(
              new FileHierarchyDirectory(
                  hierarchy,
                  this,
                  this,
                  new FinDirectory(subdir.AbsoluteSubdirPath),
                  subdir));
        }
      }

      private FileHierarchyDirectory(
          IFileHierarchy hierarchy,
          IFileHierarchyDirectory root,
          IFileHierarchyDirectory parent,
          ISystemDirectory directory,
          ISubdirPaths paths) : base(hierarchy, root, parent, directory) {
        this.Impl = directory;

        foreach (var filePath in paths.AbsoluteFilePaths) {
          this.files_.Add(
              new FileHierarchyFile(hierarchy,
                                    root,
                                    this,
                                    new FinFile(filePath)));
        }

        foreach (var subdir in paths.Subdirs) {
          this.subdirs_.Add(
              new FileHierarchyDirectory(
                  hierarchy,
                  root,
                  this,
                  new FinDirectory(subdir.AbsoluteSubdirPath),
                  subdir));
        }
      }

      private FileHierarchyDirectory(
          IFileHierarchy hierarchy,
          IFileHierarchyDirectory parent,
          ISystemDirectory directory) :
          base(hierarchy, hierarchy.Root, parent, directory) {
        this.Impl = directory;
        this.Refresh();
      }

      protected override ISystemIoObject Instance => this.Impl;
      public ISystemDirectory Impl { get; }

      public bool IsEmpty => this.Impl.IsEmpty;

      public IReadOnlyList<IFileHierarchyDirectory> GetExistingSubdirs()
        => this.subdirs_;

      public IReadOnlyList<IFileHierarchyFile> GetExistingFiles()
        => this.files_;

      public bool Refresh(bool recursive = false) {
        var didChange = false;

        var actualSubdirs = this.Impl.GetExistingSubdirs()
                                .ToArray();
        didChange |=
            ListUtil.RemoveWhere(this.subdirs_,
                                 subdir => !actualSubdirs
                                     .Contains(subdir.Impl));
        foreach (var actualSubdir in actualSubdirs) {
          if (this.subdirs_.All(subdir => !subdir.Impl.Equals(actualSubdir))) {
            this.subdirs_.Add(
                new FileHierarchyDirectory(this.Hierarchy, this, actualSubdir));
            didChange = true;
          }
        }

        var actualFiles = this.Impl.GetExistingFiles().ToArray();
        didChange |=
            ListUtil.RemoveWhere(this.files_,
                                 file => !actualFiles.Contains(file.Impl));
        foreach (var actualFile in actualFiles) {
          if (this.files_.All(file => !file.Impl.Equals(actualFile))) {
            this.files_.Add(
                new FileHierarchyFile(this.Hierarchy,
                                      this.Hierarchy.Root,
                                      this,
                                      actualFile));
            didChange = true;
          }
        }

        if (recursive) {
          foreach (var subdir in this.subdirs_) {
            didChange |= subdir.Refresh(true);
          }
        }

        return didChange;
      }

      public IFileHierarchyFile AssertGetExistingFile(string relativePath) {
        Asserts.True(this.TryToGetExistingFile(relativePath, out var outFile));
        return outFile;
      }

      public bool TryToGetExistingFile(
          string localPath,
          out IFileHierarchyFile outFile) {
        outFile = default;
        var subdirs = localPath.Split('/', '\\');

        IFileHierarchyDirectory parentDir;
        if (subdirs.Length == 1) {
          parentDir = this;
        } else {
          var parentDirPath = string.Join('/', subdirs.SkipLast(1));
          if (!this.TryToGetExistingSubdir(parentDirPath, out parentDir)) {
            return false;
          }
        }

        var match = parentDir.GetExistingFiles()
                             .FirstOrDefault(
                                 file => file.Name == subdirs.Last());
        outFile = match;
        return match != null;
      }

      public IFileHierarchyDirectory AssertGetExistingSubdir(
          string relativePath) {
        Asserts.True(this.TryToGetExistingSubdir(relativePath, out var outDir));
        return outDir;
      }

      public bool TryToGetExistingSubdir(
          string localPath,
          out IFileHierarchyDirectory outDirectory) {
        outDirectory = default;
        var subdirs = localPath.Split('/', '\\');

        IFileHierarchyDirectory current = this;
        foreach (var subdir in subdirs) {
          if (subdir == "") {
            continue;
          }

          if (subdir == "..") {
            current = Asserts.CastNonnull(current.Parent);
            continue;
          }

          var match = current.GetExistingSubdirs()
                             .FirstOrDefault(dir => dir.Name == subdir);
          if (match == null) {
            return false;
          }

          current = match;
        }

        outDirectory = current;
        return true;
      }

      public bool TryToGetExistingFileWithFileType(
          string pathWithoutExtension,
          out IFileHierarchyFile outFile,
          params string[] fileTypes) {
        outFile = default;
        var subdirs = pathWithoutExtension.Split('/', '\\');

        IFileHierarchyDirectory parentDir;
        if (subdirs.Length == 1) {
          parentDir = this;
        } else {
          var parentDirPath = string.Join('/', subdirs.SkipLast(1));
          if (!this.TryToGetExistingSubdir(parentDirPath, out parentDir)) {
            return false;
          }
        }

        var match =
            parentDir.GetExistingFiles()
                     .FirstOrDefault(
                         file => file.NameWithoutExtension == subdirs.Last() &&
                                 fileTypes.Contains(file.FileType));
        outFile = match;
        return match != null;
      }

      public IEnumerable<IFileHierarchyFile> GetFilesWithNameRecursive(
          string name) {
        name = name.ToLower();
        var stack = new FinStack<IFileHierarchyDirectory>(this);
        while (stack.TryPop(out var next)) {
          var match = next.GetExistingFiles()
                          .FirstOrDefault(file => file.Name.ToLower() == name);
          if (match != null) {
            yield return match;
          }

          stack.Push(next.GetExistingSubdirs());
        }
      }

      public IEnumerable<IFileHierarchyFile> GetFilesWithFileType(
          string fileType,
          bool includeSubdirs = false)
        => includeSubdirs
            ? FilesWithExtensionRecursive(fileType)
            : FilesWithExtension(fileType);

      public IEnumerable<IFileHierarchyFile> FilesWithExtension(
          string extension)
        => this.GetExistingFiles().Where(file => file.FileType == extension);

      public IEnumerable<IFileHierarchyFile> FilesWithExtensions(
          IEnumerable<string> extensions)
        => this.GetExistingFiles()
               .Where(
                   file => extensions.Contains(file.FileType));

      public IEnumerable<IFileHierarchyFile> FilesWithExtensions(
          string first,
          params string[] rest)
        => this.GetExistingFiles()
               .Where(file => file.FileType == first ||
                              rest.Contains(file.FileType));

      public IEnumerable<IFileHierarchyFile> FilesWithExtensionRecursive(
          string extension)
        => this.FilesWithExtension(extension)
               .Concat(
                   this.GetExistingSubdirs()
                       .SelectMany(
                           subdir
                               => subdir
                                   .FilesWithExtensionRecursive(extension)));

      public IEnumerable<IFileHierarchyFile> FilesWithExtensionsRecursive(
          IEnumerable<string> extensions)
        => this.FilesWithExtensions(extensions)
               .Concat(
                   this.GetExistingSubdirs()
                       .SelectMany(
                           subdir
                               => subdir.FilesWithExtensionsRecursive(
                                   extensions)));

      public IEnumerable<IFileHierarchyFile> FilesWithExtensionsRecursive(
          string first,
          params string[] rest)
        => this.FilesWithExtensions(first, rest)
               .Concat(
                   this.GetExistingSubdirs()
                       .SelectMany(
                           subdir
                               => subdir
                                   .FilesWithExtensionsRecursive(first, rest)));
    }

    private class FileHierarchyFile(IFileHierarchy hierarchy,
                                    IFileHierarchyDirectory root,
                                    IFileHierarchyDirectory parent,
                                    ISystemFile file)
        : BFileHierarchyIoObject(hierarchy, root, parent, file),
          IFileHierarchyFile {
      protected override ISystemIoObject Instance => this.Impl;
      public ISystemFile Impl { get; } = file;

      // File fields
      public string FileType => this.Impl.FileType;

      public string FullNameWithoutExtension
        => this.Impl.FullNameWithoutExtension;

      public string NameWithoutExtension => this.Impl.NameWithoutExtension;

      public string DisplayFullPath
        => $"//{this.Hierarchy.Name}{this.LocalPath.Replace('\\', '/')}";

      public FileSystemStream OpenRead() => this.Impl.OpenRead();
      public StreamReader OpenReadAsText() => this.Impl.OpenReadAsText();

      public T ReadNew<T>() where T : IBinaryDeserializable, new()
        => this.Impl.ReadNew<T>();

      public T ReadNew<T>(Endianness endianness)
          where T : IBinaryDeserializable, new()
        => this.Impl.ReadNew<T>(endianness);

      public T ReadNewFromText<T>() where T : ITextDeserializable, new()
        => this.Impl.ReadNewFromText<T>();

      public byte[] ReadAllBytes() => this.Impl.ReadAllBytes();
      public string ReadAllText() => this.Impl.ReadAllText();
      public string[] ReadAllLines() => this.Impl.ReadAllLines();

      public T Deserialize<T>() => this.Impl.Deserialize<T>();
    }

    public IEnumerator<IFileHierarchyDirectory> GetEnumerator() {
      var directoryQueue = new Queue<IFileHierarchyDirectory>();
      directoryQueue.Enqueue(this.Root);
      while (directoryQueue.Count > 0) {
        var directory = directoryQueue.Dequeue();

        yield return directory;

        foreach (var subdir in directory.GetExistingSubdirs()) {
          directoryQueue.Enqueue(subdir);
        }
      }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
  }
}