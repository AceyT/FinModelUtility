﻿using fin.io;
using fin.io.archive;
using fin.util.strings;

using uni.platforms.gcn.tools;

namespace uni.platforms.gcn {
  public class GcnFileHierarchyExtractor {
    private readonly RarcDump rarcDump_ = new();
    private readonly RelDump relDump_ = new();
    private readonly Yay0Dec yay0Dec_ = new();
    private readonly Yaz0Dec yaz0Dec_ = new();

    public bool TryToExtractFromGame(
        string gameName,
        out IFileHierarchy fileHierarchy)
      => this.TryToExtractFromGame(gameName,
                                   Options.Standard(),
                                   out fileHierarchy);

    public bool TryToExtractFromGame(
        string gameName,
        Options options,
        out IFileHierarchy fileHierarchy) {
      if (!this.TryToFindRom_(gameName, out var romFile)) {
        fileHierarchy = default;
        return false;
      }

      fileHierarchy = this.ExtractFromRom_(romFile, options);
      return true;
    }

    private bool TryToFindRom_(string gameName, out ISystemFile romFile)
      => DirectoryConstants.ROMS_DIRECTORY
                           .TryToGetExistingFileWithFileType(
                               gameName,
                               out romFile,
                               ".ciso",
                               ".nkit.iso",
                               ".iso",
                               ".gcm");

    public IFileHierarchy ExtractFromRom_(
        ISystemFile romFile,
        Options options) {
      var directory = romFile.AssertGetParent()
                             .GetOrCreateSubdir(romFile.Name.SubstringUpTo("."));

      if (new SubArchiveExtractor().TryToExtractIntoNewDirectory<GcmReader>(
              romFile,
              directory) == ArchiveExtractionResult.FAILED) {
        throw new Exception();
      }

      var fileHierarchy = new FileHierarchy(directory);
      var hasChanged = false;

      // Decompresses all of the archives,
      foreach (var subdir in fileHierarchy) {
        var didDecompress = false;

        // Decompresses files
        foreach (var file in subdir.FilesWithExtensions(
                     options.Yay0DecExtensions)) {
          didDecompress |=
              this.yay0Dec_.Run(file, options.ContainerCleanupEnabled);
        }

        foreach (var file in subdir.FilesWithExtensions(
                     options.Yaz0DecExtensions)) {
          didDecompress |=
              this.yaz0Dec_.Run(file, options.ContainerCleanupEnabled);
        }

        // Updates to see any new decompressed files.
        if (didDecompress) {
          hasChanged = true;
          subdir.Refresh();
        }


        // Dumps any REL files
        var didDump = false;
        var relFiles =
            subdir.Files.Where(
                      file => file.Name.Contains(".rel") &&
                              file.Extension == ".rarc")
                  .ToArray();
        foreach (var relFile in relFiles) {
          var prefix = StringUtil.SubstringUpTo(relFile.Name, ".rel");
          var mapFile =
              subdir.Files.Single(
                  file => file.Name.StartsWith(prefix) &&
                          file.Extension == ".map");
          didDump |=
              this.relDump_.Run(relFile,
                                mapFile,
                                options.ContainerCleanupEnabled);
        }

        // Dumps any ARC/RARC files.
        var arcFiles =
            subdir.Files
                  .Where(file => !relFiles.Contains(file))
                  .Where(file => options.RarcDumpExtensions.Contains(
                             file.Extension))
                  .ToArray();
        foreach (var arcFile in arcFiles) {
          didDump |=
              this.rarcDump_.Run(arcFile,
                                 options.ContainerCleanupEnabled,
                                 options.RarcDumpPruneNames);
        }

        // Updates to see any new dumped directories.
        if (didDump) {
          hasChanged = true;
          subdir.Refresh();
        }
      }


      if (hasChanged) {
        fileHierarchy.Root.Refresh(true);
      }

      return fileHierarchy;
    }

    public class Options {
      private readonly HashSet<string> rarcDumpExtensions_ = new();
      private readonly HashSet<string> rarcDumpPruneNames_ = new();
      private readonly HashSet<string> yay0DecExtensions_ = new();
      private readonly HashSet<string> yaz0DecExtensions_ = new();

      private Options() {
        this.RarcDumpExtensions = this.rarcDumpExtensions_;
        this.RarcDumpPruneNames = this.rarcDumpPruneNames_;
        this.Yay0DecExtensions = this.yay0DecExtensions_;
        this.Yaz0DecExtensions = this.yaz0DecExtensions_;
      }

      public static Options Empty() => new();

      public static Options Standard()
        => new Options().UseYaz0DecForExtensions(".szs")
                        .UseRarcDumpForExtensions(".rarc")
                        .EnableContainerCleanup(true);

      public IReadOnlySet<string> RarcDumpExtensions { get; }
      public IReadOnlySet<string> RarcDumpPruneNames { get; }
      public IReadOnlySet<string> Yaz0DecExtensions { get; }
      public IReadOnlySet<string> Yay0DecExtensions { get; }
      public bool ContainerCleanupEnabled { get; private set; }

      public Options UseRarcDumpForExtensions(
          string first,
          params string[] rest) {
        this.rarcDumpExtensions_.Add(first);
        foreach (var o in rest) {
          this.rarcDumpExtensions_.Add(o);
        }

        return this;
      }

      public Options PruneRarcDumpNames(
          string first,
          params string[] rest) {
        this.rarcDumpPruneNames_.Add(first);
        foreach (var o in rest) {
          this.rarcDumpPruneNames_.Add(o);
        }

        return this;
      }

      public Options UseYay0DecForExtensions(
          string first,
          params string[] rest) {
        this.yay0DecExtensions_.Add(first);
        foreach (var o in rest) {
          this.yay0DecExtensions_.Add(o);
        }

        return this;
      }

      public Options UseYaz0DecForExtensions(
          string first,
          params string[] rest) {
        this.yaz0DecExtensions_.Add(first);
        foreach (var o in rest) {
          this.yaz0DecExtensions_.Add(o);
        }

        return this;
      }

      public Options EnableContainerCleanup(bool enabled) {
        this.ContainerCleanupEnabled = enabled;
        return this;
      }
    }
  }
}