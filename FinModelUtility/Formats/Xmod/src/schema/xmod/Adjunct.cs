﻿using fin.util.asserts;

using schema.text;
using schema.text.reader;

namespace xmod.schema.xmod {

  public class Adjunct : ITextDeserializable {
    public int PositionIndex { get; set; }
    public int NormalIndex { get; set; }
    public int ColorIndex { get; set; }
    public int Uv1Index { get; set; }
    public int MatrixIndex { get; set; }

    public void Read(ITextReader tr) {
      tr.SkipManyIfPresent(TextReaderConstants.WHITESPACE_STRINGS);
      tr.AssertString("adj");

      var indices = tr.ReadInt32s(TextReaderConstants.WHITESPACE_STRINGS, TextReaderConstants.NEWLINE_STRINGS);
      Asserts.Equal(6, indices.Length);

      PositionIndex = indices[0];
      NormalIndex = indices[1];
      ColorIndex = indices[2];
      Uv1Index = indices[3];

      // TODO: This might not be correct
      var uv2Index = indices[4];

      MatrixIndex = indices[5];

      tr.SkipManyIfPresent(TextReaderConstants.WHITESPACE_STRINGS);
    }
  }
}
