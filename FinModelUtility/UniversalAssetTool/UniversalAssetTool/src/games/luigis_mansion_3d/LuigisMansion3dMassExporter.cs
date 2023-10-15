﻿using cmb.api;

namespace uni.games.luigis_mansion_3d {
  public class LuigisMansion3dMassExporter : IMassExporter {
    public void ExportAll()
      => ExporterUtil.ExportAllForCli(new LuigisMansion3dModelAnnotatedFileGatherer(),
                                  new CmbModelImporter());
  }
}