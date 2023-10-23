﻿using System;
using System.Drawing;
using System.IO;

using CommunityToolkit.HighPerformance;

using fin.util.hash;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace fin.image.formats {
  public abstract class BImage<TPixel> : IImage<TPixel>
      where TPixel : unmanaged, IPixel<TPixel> {
    protected BImage(PixelFormat format) {
      this.PixelFormat = format;
    }

    ~BImage() => this.ReleaseUnmanagedResources_();

    public void Dispose() {
      this.ReleaseUnmanagedResources_();
      GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources_() => this.Impl.Dispose();

    protected abstract Image<TPixel> Impl { get; }

    public PixelFormat PixelFormat { get; }
    public int Width => this.Impl.Width;
    public int Height => this.Impl.Height;

    public abstract void Access(IImage.AccessHandler accessHandler);
    public abstract bool HasAlphaChannel { get; }

    public Bitmap AsBitmap() => FinImage.ConvertToBitmap(this);

    public void ExportToStream(Stream stream, LocalImageFormat imageFormat)
      => this.Impl.Save(
          stream,
          FinImage.ConvertFinImageFormatToImageSharpEncoder(imageFormat));

    public FinImageLock<TPixel> Lock() => new(Impl);


    public override unsafe bool Equals(object? obj) {
      if (Object.ReferenceEquals(this, obj)) {
        return true;
      }

      if (obj is IImage otherGeneric) {
        if (this.Width != otherGeneric.Width ||
            this.Height != otherGeneric.Height) {
          return false;
        }

        if (obj is IImage<TPixel> otherSame) {
          var pixelCount = this.Width * this.Height;

          using var fastLock = this.Lock();
          var ptr = fastLock.pixelScan0;
          var span = new Span<TPixel>(ptr, pixelCount).AsBytes();

          using var otherFastLock = otherSame.Lock();
          var otherPtr = otherFastLock.pixelScan0;
          var otherSpan = new Span<TPixel>(otherPtr, pixelCount).AsBytes();

          for (var i = 0; i < span.Length; ++i) {
            if (span[i] != otherSpan[i]) {
              return false;
            }
          }

          return true;
        }

        bool match = true;
        this.Access(thisAccessor => {
          otherGeneric.Access(otherAccessor => {
            for (var y = 0; y < this.Height; ++y) {
              for (var x = 0; x < this.Width; ++x) {
                thisAccessor(x,
                             y,
                             out var thisR,
                             out var thisG,
                             out var thisB,
                             out var thisA);
                otherAccessor(x,
                              y,
                              out var otherR,
                              out var otherG,
                              out var otherB,
                              out var otherA);

                if (thisR != otherR ||
                    thisG != otherG ||
                    thisB != otherG ||
                    thisA != otherA) {
                  match = false;
                  return;
                }
              }
            }
          });
        });

        return match;
      }

      return false;
    }

    private int? cachedHash_ = null;

    public override unsafe int GetHashCode() {
      if (this.cachedHash_ != null) {
        return this.cachedHash_.Value;
      }

      var pixelCount = this.Width * this.Height;

      using var fastLock = this.Lock();
      var ptr = fastLock.pixelScan0;
      var span = new Span<TPixel>(ptr, pixelCount).AsBytes();

      var hash = FluentHash.Start();
      hash.With(span);

      this.cachedHash_ = hash;
      return hash;
    }
  }
}