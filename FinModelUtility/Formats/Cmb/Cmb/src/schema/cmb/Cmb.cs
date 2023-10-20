﻿using System;

using cmb.schema.cmb.luts;
using cmb.schema.cmb.mats;
using cmb.schema.cmb.qtrs;
using cmb.schema.cmb.skl;
using cmb.schema.cmb.sklm;
using cmb.schema.cmb.tex;
using cmb.schema.cmb.vatr;

using fin.schema.data;
using fin.util.strings;

using schema.binary;
using schema.binary.attributes;

namespace cmb.schema.cmb {
  [Endianness(Endianness.LittleEndian)]
  public class Cmb : IBinaryDeserializable {
    public long startOffset;

    public readonly CmbHeader header = new();

    private const int TWEAK_AUTO_SIZE = -8;

    public readonly AutoStringMagicUInt32SizedSection<Skl> skl
        = new("skl" + AsciiUtil.GetChar(0x20));

    public readonly AutoStringMagicUInt32SizedSection<Qtrs> qtrs
        = new("qtrs");

    public AutoStringMagicUInt32SizedSection<Mats> mats { get; set; } =
      new("mats") {
          TweakReadSize = TWEAK_AUTO_SIZE,
      };

    public readonly AutoStringMagicUInt32SizedSection<Tex> tex
        = new("tex" + AsciiUtil.GetChar(0x20));

    public readonly AutoStringMagicUInt32SizedSection<Sklm> sklm =
        new("sklm");

    public readonly AutoStringMagicUInt32SizedSection<Luts> luts
        = new("luts");

    public readonly Vatr vatr = new();

    public Cmb(IBinaryReader br) => this.Read(br);

    public void Read(IBinaryReader br) {
      long startOff = 0;

      if (br.ReadString(4) == "ZSI" + AsciiUtil.GetChar(1)) {
        br.Position = 16;

        while (true) {
          var cmd0 = br.ReadUInt32();
          var cmd1 = br.ReadUInt32();

          var cmdType = cmd0 & 0xFF;

          if (cmdType == 0x14) {
            break;
          }

          if (cmdType == 0x0A) {
            br.Position = cmd1 + 20;

            var entryOfs = br.ReadUInt32();
            br.Position = entryOfs + 24;

            var cmbOfs = br.ReadUInt32();
            br.Position = cmbOfs + 16;

            startOff = br.Position;
            break;
          }
        }
      }

      this.startOffset = br.Position = startOff;

      this.header.Read(br);

      br.Position = startOff + this.header.sklOffset;
      this.skl.Read(br);

      if (CmbHeader.Version > Version.OCARINA_OF_TIME_3D) {
        br.Position = startOff + this.header.qtrsOffset;
        this.qtrs.Read(br);
      }

      br.Position = startOff + this.header.matsOffset;
      this.mats.Read(br);

      br.Position = startOff + this.header.texOffset;
      this.tex.Read(br);

      br.Position = startOff + this.header.sklmOffset;
      this.sklm.Read(br);

      br.Position = startOff + this.header.lutsOffset;
      this.luts.Read(br);

      br.Position = startOff + this.header.vatrOffset;
      this.vatr.Read(br);

      // Add face indices to primitive sets
      var sklm = this.sklm.Data;
      foreach (var shape in sklm.shapes.shapes) {
        foreach (var pset in shape.primitiveSets) {
          var primitive = pset.primitive;
          // # Always * 2 even if ubyte is used...
          br.Position = startOff +
                       this.header.faceIndicesOffset +
                       2 * primitive.offset;

          primitive.indices = new uint[primitive.indicesCount];
          for (var i = 0; i < primitive.indicesCount; ++i) {
            switch (primitive.dataType) {
              case DataType.UByte: {
                primitive.indices[i] = br.ReadByte();
                break;
              }
              case DataType.UShort: {
                primitive.indices[i] = br.ReadUInt16();
                break;
              }
              default: {
                throw new NotImplementedException();
              }
            }
          }
        }
      }
    }
  }
}


/*pset.primitive.indices = 
[int(readDataType(f, pset.primitive.dataType)) for _ in
range(pset.primitive.indicesCount)]*/