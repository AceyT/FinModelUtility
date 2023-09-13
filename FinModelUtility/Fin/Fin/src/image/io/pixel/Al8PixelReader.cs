﻿using fin.image.formats;

using schema.binary;

using SixLabors.ImageSharp.PixelFormats;

namespace fin.image.io.pixel {
  /// <summary>
  ///   Helper class for reading 8-bit luminance/alpha pixels.
  /// </summary>
  public class Al8PixelReader : IPixelReader<La16> {
    public IImage<La16> CreateImage(int width, int height)
      => new La16Image(PixelFormat.LA44, width, height);

    public unsafe void Decode(IEndianBinaryReader er,
                              La16* scan0,
                              int offset) {
      var value = er.ReadByte();

      var luminance = (byte) ((value >> 4) * 17);
      var alpha = (byte) ((value & 0xF) * 17);

      scan0[offset] = new La16(luminance, alpha);
    }
  }
}