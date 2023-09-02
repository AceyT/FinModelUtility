﻿using fin.audio.ogg;
using fin.io;
using fin.io.bundles;

using glo.api;

using uni.platforms.desktop;

namespace uni.games.glover {
  public class GloverModelFileGatherer : IFileBundleGatherer<IFileBundle> {
    public IEnumerable<IFileBundle> GatherFileBundles() {
      if (!SteamUtils.TryGetGameDirectory("Glover",
                                          out var gloverSteamDirectory)) {
        yield break;
      }

      var gloverFileHierarchy = new FileHierarchy(gloverSteamDirectory);

      var dataDirectory = gloverFileHierarchy.Root.AssertGetExistingSubdir("data");
      var topLevelBgmDirectory = dataDirectory.AssertGetExistingSubdir("bgm");
      foreach (var bgmFile in topLevelBgmDirectory.GetExistingFiles()) {
        yield return new OggAudioFileBundle(bgmFile);
      }

      var topLevelObjectDirectory = dataDirectory.AssertGetExistingSubdir("objects");
      foreach (var objectDirectory in topLevelObjectDirectory.GetExistingSubdirs()) {
        foreach (var fileBundle in this.AddObjectDirectory_(
                     gloverFileHierarchy,
                     objectDirectory)) {
          yield return fileBundle;
        }
      }
    }

    private IEnumerable<IFileBundle> AddObjectDirectory_(
        IFileHierarchy gloverFileHierarchy,
        IFileHierarchyDirectory objectDirectory) {
      var objectFiles = objectDirectory.FilesWithExtension(".glo");

      var gloverSteamDirectory = gloverFileHierarchy.Root;
      var textureDirectories = gloverSteamDirectory
                               .AssertGetExistingSubdir("data/textures/generic")
                               .GetExistingSubdirs().ToList();

      try {
        var levelTextureDirectory = gloverSteamDirectory.AssertGetExistingSubdir(
            objectDirectory.LocalPath.Replace("data\\objects",
                                              "data\\textures"));
        textureDirectories.Add(levelTextureDirectory);
        textureDirectories.AddRange(levelTextureDirectory.GetExistingSubdirs());
      } catch {
        // ignored
      }

      foreach (var objectFile in objectFiles) {
        yield return new GloModelFileBundle(objectFile, textureDirectories);
      }
    }
  }
}