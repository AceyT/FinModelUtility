﻿using System;

using cmb.schema.cmb;

using fin.image;
using fin.image.io;
using fin.image.io.pixel;
using fin.image.io.tile;

using schema.binary;

using SixLabors.ImageSharp.PixelFormats;

namespace cmb.image {
  /// <summary>
  ///   Stolen from:
  ///   https://github.com/magcius/noclip.website/blob/master/src/oot3d/pica_texture.ts
  /// </summary>
  public class CmbImageReader : IImageReader {
    private readonly IImageReader impl_;

    public CmbImageReader(int width, int height, GlTextureFormat format) {
      this.impl_ = this.CreateImpl_(width, height, format);
    }

    private IImageReader CreateImpl_(int width,
                                     int height,
                                     GlTextureFormat format) {
      if (format.IsEtc1(out var hasAlpha)) {
        return TiledImageReader.New(width,
                                    height,
                                    new Etc1TileReader(hasAlpha));
      }

      var blockWidth = 8;
      var blockHeight = 8;
      var tilePixelIndexer = new MortonPixelIndexer();

      if (format.IsRgb()) {
        IPixelReader<Rgb24> pixelReader = format switch {
            GlTextureFormat.RGB8   => new Rgb24PixelReader(),
            GlTextureFormat.RGB565 => new Rgb565PixelReader(),
        };

        return TiledImageReader.New(width,
                                    height,
                                    blockWidth,
                                    blockHeight,
                                    tilePixelIndexer,
                                    pixelReader);
      }

      if (format.IsRgba()) {
        IPixelReader<Rgba32> pixelReader = format switch {
            GlTextureFormat.RGBA8    => new Rgba32PixelReader(),
            GlTextureFormat.RGBA4444 => new Rgba4444PixelReader(),
            GlTextureFormat.RGBA5551 => new Argb1555PixelReader(),
        };

        return TiledImageReader.New(width,
                                    height,
                                    blockWidth,
                                    blockHeight,
                                    tilePixelIndexer,
                                    pixelReader);
      }

      if (format.IsIntensity()) {
        IPixelReader<L8> pixelReader = format switch {
            GlTextureFormat.L4     => new L4PixelReader(),
            GlTextureFormat.L8     => new L8PixelReader(),
            GlTextureFormat.Gas    => new L8PixelReader(),
            GlTextureFormat.Shadow => new L8PixelReader(),
        };

        return TiledImageReader.New(width,
                                    height,
                                    blockWidth,
                                    blockHeight,
                                    tilePixelIndexer,
                                    pixelReader);
      }

      if (format.IsIntensityAlpha()) {
        IPixelReader<La16> pixelReader = format switch {
            GlTextureFormat.LA8 => new La16PixelReader(),
        };

        return TiledImageReader.New(width,
                                    height,
                                    blockWidth,
                                    blockHeight,
                                    tilePixelIndexer,
                                    pixelReader);
      }

      if (format.IsAlpha()) {
        IPixelReader<La16> pixelReader = format switch {
            GlTextureFormat.A8 => new A8PixelReader(),
        };

        return TiledImageReader.New(width,
                                    height,
                                    blockWidth,
                                    blockHeight,
                                    tilePixelIndexer,
                                    pixelReader);
      }

      throw new NotImplementedException();
    }

    public IImage ReadImage(IBinaryReader br) => this.impl_.ReadImage(br);
  }
}