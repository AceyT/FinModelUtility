﻿using cmb.api;

using fin.model.io;
using fin.model.io.importers.assimp;

using glo.api;

using jsystem.api;

using mod.api;

namespace uni.cli {
  public static class PluginUtil {
    public static IReadOnlyList<IModelImporterPlugin> Plugins { get; } =
      new IModelImporterPlugin[] {
          new AssimpModelImporterPlugin(),
          new BmdModelImporterPlugin(),
          new CmbModelImporterPlugin(),
          new GloModelImporterPlugin(),
          new ModModelImporterPlugin(),
      };
  }
}