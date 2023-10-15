﻿using hw.api;

namespace uni.games.halo_wars {
  internal class HaloWarsMassExporter : IMassExporter {
    public void ExportAll()
      => ExporterUtil.ExportAllForCli(
        new HaloWarsModelAnnotatedFileGatherer(),
        new HaloWarsModelImporter());

      /*new HaloWarsTools.Program().Run(
          DirectoryConstants.ROMS_DIRECTORY.GetSubdir("halo_wars", true).FullName,
          DirectoryConstants.OUT_DIRECTORY.GetSubdir("halo_wars", true).FullName);*/
  }
}