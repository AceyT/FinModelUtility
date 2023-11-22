﻿using System.IO;

using Assimp;

using fin.model.io.exporters.assimp;
using fin.io;
using fin.io.bundles;
using fin.log;
using fin.model;
using fin.model.io;
using fin.model.io.exporters;
using fin.model.io.exporters.assimp.indirect;
using fin.model.io.importers;
using fin.util.asserts;
using fin.util.linq;

using uni.config;
using uni.model;
using uni.msg;
using uni.thirdparty;

namespace uni.games {
  public static class ExporterUtil {
    static ExporterUtil() {
      ExporterUtil.logger_ = Logging.Create("exportor");
    }

    private static readonly ILogger logger_;

    public static bool CheckIfFilesAlreadyExist(
        IEnumerable<ISystemFile> outputFiles,
        out IReadOnlyList<ISystemFile> existingOutputFiles) {
      existingOutputFiles =
          outputFiles.Where(file => file.Exists).ToArray();
      return existingOutputFiles.Count > 0;
    }

    public static bool CheckIfModelFileBundlesAlreadyExported(
        IEnumerable<IAnnotatedFileBundle> modelFileBundles,
        IReadOnlyList<string> extensions,
        out IReadOnlyList<IAnnotatedFileBundle> existingModelFileBundles) {
      existingModelFileBundles =
          modelFileBundles
              .Where(mfb => CheckIfModelFileBundleAlreadyExported(
                         mfb,
                         extensions))
              .ToArray();
      return existingModelFileBundles.Count > 0;
    }

    public static bool CheckIfModelFileBundleAlreadyExported(
        IAnnotatedFileBundle annotatedModelFileBundle,
        IEnumerable<string> extensions) {
      // TODO: Clean this up!!
      var bundle = annotatedModelFileBundle.FileBundle;
      var mainFile = bundle.MainFile;

      var parentOutputDirectory =
          ExtractorUtil.GetOutputDirectoryForFileBundle(
              annotatedModelFileBundle);
      var outputDirectory = new FinDirectory(
          Path.Join(parentOutputDirectory.FullPath,
                    mainFile.NameWithoutExtension));

      if (outputDirectory.Exists) {
        return extensions.All(
            extension => outputDirectory
                         .GetExistingFiles()
                         .Where(file => extensions.Contains(file.FileType))
                         .Any(file => file.NameWithoutExtension ==
                                      mainFile.NameWithoutExtension));
      }

      return false;
    }

    public enum ExporterPromptChoice {
      CANCEL,
      SKIP_EXISTING,
      OVERWRITE_EXISTING,
    }

    public static ExporterPromptChoice PromptIfFilesAlreadyExist(
        IReadOnlyList<ISystemFile> outputFiles) {
      if (CheckIfFilesAlreadyExist(outputFiles, out var existingOutputFiles)) {
        if (outputFiles.Count == 1) {
          var result =
              MessageBox.Show(
                  $"Output file \"{existingOutputFiles.First()}\" already exists. Would you like to overwrite it?",
                  "Output file already exists!",
                  MessageBoxButtons.YesNo,
                  MessageBoxIcon.Warning,
                  MessageBoxDefaultButton.Button1);
          return result switch {
              DialogResult.Yes => ExporterPromptChoice.OVERWRITE_EXISTING,
              DialogResult.No  => ExporterPromptChoice.CANCEL,
          };
        } else {
          var result =
              MessageBox.Show(
                  $"\"{existingOutputFiles.Count()}\" output files already exist. Would you like to continue exporting and overwrite them?",
                  "Some output files already exist!",
                  MessageBoxButtons.YesNo,
                  MessageBoxIcon.Warning,
                  MessageBoxDefaultButton.Button1);
          return result switch {
              DialogResult.Yes    => ExporterPromptChoice.OVERWRITE_EXISTING,
              DialogResult.No     => ExporterPromptChoice.SKIP_EXISTING,
              DialogResult.Cancel => ExporterPromptChoice.CANCEL,
          };
        }
      }

      return ExporterPromptChoice.SKIP_EXISTING;
    }

    public static ExporterPromptChoice
        PromptIfModelFileBundlesAlreadyExported(
            IReadOnlyList<IAnnotatedFileBundle> modelFileBundles,
            IReadOnlyList<string> extensions) {
      if (ExporterUtil.CheckIfModelFileBundlesAlreadyExported(
              modelFileBundles,
              extensions,
              out var existingOutputFiles)) {
        var totalCount = modelFileBundles.Count;
        if (totalCount == 1) {
          var result =
              MessageBox.Show(
                  $"Model defined in \"{existingOutputFiles.First().FileBundle.DisplayFullPath}\" has already been exported. Would you like to overwrite it?",
                  "Model has already been exported!",
                  MessageBoxButtons.YesNo,
                  MessageBoxIcon.Warning,
                  MessageBoxDefaultButton.Button1);
          return result switch {
              DialogResult.Yes => ExporterPromptChoice.OVERWRITE_EXISTING,
              DialogResult.No  => ExporterPromptChoice.CANCEL,
          };
        } else {
          var existingCount = existingOutputFiles.Count();
          var result =
              MessageBox.Show(
                  $"{existingCount} model{(existingCount != 1 ? "s have" : " has")} already been exported. Select 'Yes' to overwrite them, 'No' to skip them, or 'Cancel' to abort this operation.",
                  $"{existingCount}/{totalCount} models have already been exported!",
                  MessageBoxButtons.YesNoCancel,
                  MessageBoxIcon.Warning,
                  MessageBoxDefaultButton.Button1);
          return result switch {
              DialogResult.Yes    => ExporterPromptChoice.OVERWRITE_EXISTING,
              DialogResult.No     => ExporterPromptChoice.SKIP_EXISTING,
              DialogResult.Cancel => ExporterPromptChoice.CANCEL,
          };
        }
      }

      return ExporterPromptChoice.SKIP_EXISTING;
    }

    public static void ExportAllForCli<T>(
        IAnnotatedFileBundleGatherer<T> gatherer,
        IModelImporter<T> reader)
        where T : IModelFileBundle
      => ExporterUtil.ExportAllForCli_(gatherer.GatherFileBundles(),
                                       reader,
                                       Config.Instance.ExporterSettings
                                             .ExportedFormats,
                                       false);

    public static void ExportAllForCli<T>(
        IAnnotatedFileBundleGatherer gatherer,
        IModelImporter<T> reader)
        where T : IModelFileBundle
      => ExporterUtil.ExportAllForCli_(
          gatherer.GatherFileBundles(),
          reader,
          Config.Instance.ExporterSettings.ExportedFormats,
          false);

    private static void ExportAllForCli_<T>(
        IEnumerable<IAnnotatedFileBundle> fileBundles,
        IModelImporter<T> reader,
        IReadOnlyList<string> extensions,
        bool overwriteExistingFiles)
        where T : IModelFileBundle
      => ExporterUtil.ExportAllForCli_(
          fileBundles.WhereIs<IAnnotatedFileBundle, IAnnotatedFileBundle<T>>(),
          reader,
          extensions,
          overwriteExistingFiles);

    private static void ExportAllForCli_<T>(
        IEnumerable<IAnnotatedFileBundle<T>> modelFileBundles,
        IModelImporter<T> reader,
        IReadOnlyList<string> extensions,
        bool overwriteExistingFiles)
        where T : IModelFileBundle {
      var bundlesArray = modelFileBundles.ToArray();
      Asserts.True(bundlesArray.Length > 0,
                   "Expected to find bundles for the current ROM. Does the file exist, and was it exported correctly?");

      foreach (var modelFileBundle in bundlesArray) {
        ExporterUtil.Export(modelFileBundle,
                            reader,
                            extensions,
                            overwriteExistingFiles);
      }
    }


    public static void ExportAll<T>(
        IEnumerable<IAnnotatedFileBundle> fileBundles,
        IModelImporter<T> reader,
        IProgress<(float, T?)> progress,
        CancellationTokenSource cancellationTokenSource,
        IReadOnlyList<string> extensions,
        bool overwriteExistingFiles)
        where T : IModelFileBundle {
      var fileBundleArray = fileBundles
                            .WhereIs<IAnnotatedFileBundle,
                                IAnnotatedFileBundle<T>>()
                            .ToArray();
      for (var i = 0; i < fileBundleArray.Length; ++i) {
        if (cancellationTokenSource.IsCancellationRequested) {
          break;
        }

        var modelFileBundle = fileBundleArray[i];
        progress.Report((i * 1f / fileBundleArray.Length,
                         modelFileBundle.TypedFileBundle));
        ExporterUtil.Export(modelFileBundle,
                            reader,
                            extensions,
                            overwriteExistingFiles);
      }

      progress.Report((1, default));
    }

    public static void Export<T>(IAnnotatedFileBundle<T> modelFileBundle,
                                 IModelImporter<T> reader,
                                 IReadOnlyList<string> extensions,
                                 bool overwriteExistingFile)
        where T : IModelFileBundle {
      ExporterUtil.Export(modelFileBundle,
                          () => reader.ImportModel(
                              modelFileBundle.TypedFileBundle),
                          extensions,
                          overwriteExistingFile);
    }

    public static void Export<T>(IAnnotatedFileBundle<T> modelFileBundle,
                                 Func<IModel> loaderHandler,
                                 IReadOnlyList<string> extensions,
                                 bool overwriteExistingFile)
        where T : IModelFileBundle {
      var mainFile = Asserts.CastNonnull(modelFileBundle.FileBundle.MainFile);

      var parentOutputDirectory =
          ExtractorUtil
              .GetOutputDirectoryForFileBundle(modelFileBundle);
      var outputDirectory = new FinDirectory(
          Path.Join(parentOutputDirectory.FullPath,
                    mainFile.NameWithoutExtension));

      Export<T>(modelFileBundle,
                loaderHandler,
                outputDirectory,
                extensions,
                overwriteExistingFile);
    }

    public static void Export<T>(IAnnotatedFileBundle<T> modelFileBundle,
                                 Func<IModel> loaderHandler,
                                 ISystemDirectory outputDirectory,
                                 IReadOnlyList<string> extensions,
                                 bool overwriteExistingFile,
                                 string? overrideName = null)
        where T : IModelFileBundle
      => Export(modelFileBundle,
                loaderHandler,
                outputDirectory,
                extensions.Select(AssimpUtil.GetExportFormatFromExtension)
                          .ToArray(),
                overwriteExistingFile,
                overrideName);


    public static void Export<T>(
        IAnnotatedFileBundle<T> annotatedModelFileBundle,
        Func<IModel> loaderHandler,
        ISystemDirectory outputDirectory,
        IReadOnlyList<ExportFormatDescription> formats,
        bool overwriteExistingFile,
        string? overrideName = null)
        where T : IModelFileBundle {
      var modelFileBundle = annotatedModelFileBundle.TypedFileBundle;
      var mainFile = Asserts.CastNonnull(modelFileBundle.MainFile);
      var name = overrideName ?? mainFile.NameWithoutExtension;

      if (modelFileBundle.UseLowLevelExporter) {
        formats = new[] { AssimpUtil.GetExportFormatFromExtension(".gltf") };
      }

      if (!overwriteExistingFile && CheckIfModelFileBundleAlreadyExported(
              annotatedModelFileBundle,
              formats.Select(format => $".{format.FileExtension}"))) {
        MessageUtil.LogAlreadyProcessed(ExporterUtil.logger_, mainFile);
        return;
      }

      outputDirectory.Create();
      MessageUtil.LogExporting(ExporterUtil.logger_, mainFile);

      try {
        var model = loaderHandler();

        new AssimpIndirectModelExporter {
            LowLevel = modelFileBundle.UseLowLevelExporter,
            ForceGarbageCollection = modelFileBundle.ForceGarbageCollection,
        }.ExportFormats(new ModelExporterParams {
                            OutputFile = new FinFile(
                                Path.Join(outputDirectory.FullPath,
                                          name + ".foo")),
                            Model = model,
                            Scale = new ScaleSource(
                                    Config.Instance.ExporterSettings
                                          .ExportedModelScaleSource)
                                .GetScale(model,
                                          annotatedModelFileBundle
                                              .TypedFileBundle)
                        },
                        formats,
                        Config.Instance.ExporterSettings.ExportAllTextures);

        if (Config.Instance.ThirdPartySettings
                  .ExportBoneScaleAnimationsSeparately) {
          new BoneScaleAnimationExporter().Export(
              new FinFile(Path.Join(outputDirectory.FullPath,
                                    name + "_bone_scale_animations.lua")),
              model);
        }
      } catch (Exception e) {
        ExporterUtil.logger_.LogError(e.ToString());
      }

      ExporterUtil.logger_.LogInformation(" ");
    }
  }
}