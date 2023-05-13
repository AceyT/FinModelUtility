﻿using fin.util.strings;

using schema.binary;
using schema.binary.attributes.align;
using schema.binary.attributes.sequence;


namespace cmb.schema.cmb {
  [BinarySchema]
  public partial class Shp : IBinaryConvertible {
    private readonly string magic_ = "shp" + AsciiUtil.GetChar(0x20);

    public uint chunkSize;
    private uint shapeCount_;

    // M-1:
    // No idea... but it does something to materials and it's never used on ANY model but link's in OoT3D
    // Set to 0x58 on "link_v2.cmb"
    public uint flags;

    [RSequenceLengthSource(nameof(shapeCount_))]
    private ushort[] shapeOffsets_;
    
    [Align(4)]
    [RSequenceLengthSource(nameof(shapeCount_))]
    public Sepd[] shapes;
  }
}
