﻿using CommunityToolkit.HighPerformance;

using fin.color;
using fin.model;
using fin.util.asserts;
using fin.util.color;
using fin.util.enumerables;
using fin.util.enums;

using gx;

using schema.binary;

using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace dat.schema {
  [Flags]
  public enum PObjFlags : ushort {
    BIT_1 = (1 << 0),
    BIT_2 = (1 << 1),
    UNKNOWN2 = (1 << 2),
    ANIM = (1 << 3),

    BIT_5 = (1 << 4),
    BIT_6 = (1 << 5),
    BIT_7 = (1 << 6),
    BIT_8 = (1 << 7),
    BIT_9 = (1 << 8),
    BIT_10 = (1 << 9),
    BIT_11 = (1 << 10),
    BIT_12 = (1 << 11),

    OBJTYPE_SHAPEANIM = 1 << 12,
    OBJTYPE_ENVELOPE = 1 << 13,

    CULLBACK = (1 << 14),
    CULLFRONT = (1 << 15)
  }

  /// <summary>
  ///   Polygon object.
  /// </summary>
  public partial class PObj : IBinaryDeserializable {
    private readonly Dat dat_;

    public PObj(Dat dat) {
      this.dat_ = dat;
    }

    [BinarySchema]
    public partial class PObjHeader : IBinaryDeserializable {
      public uint StringOffset { get; set; }
      public uint NextPObjOffset { get; set; }
      public uint VertexDescriptorListOffset { get; set; }
      public PObjFlags Flags { get; set; }
      public ushort DisplayListSize { get; set; }
      public uint DisplayListOffset { get; set; }
      public uint WeightListOffset { get; set; }
    }

    public PObjHeader Header { get; } = new();
    public PObj? NextPObj { get; private set; }

    public List<VertexDescriptor> VertexDescriptors { get; } = new();
    public List<DatPrimitive> Primitives { get; } = new();

    public VertexSpace VertexSpace { get; private set; }
    public List<IList<PObjWeight>>? Weights { get; private set; }

    public void Read(IBinaryReader br) {
      this.Header.Read(br);

      // TODO: Read weights
      // https://github.com/jam1garner/Smash-Forge/blob/c0075bca364366bbea2d3803f5aeae45a4168640/Smash%20Forge/Filetypes/Melee/DAT.cs#L1515C21-L1515C38

      br.Position = this.Header.VertexDescriptorListOffset;

      // Reads vertex descriptors
      while (true) {
        var vertexDescriptor = new VertexDescriptor();
        vertexDescriptor.Read(br);

        if (vertexDescriptor.Attribute == GxAttribute.NULL) {
          break;
        }

        this.VertexDescriptors.Add(vertexDescriptor);
      }

      this.ReadDisplayList_(br);

      if (this.Header.NextPObjOffset != 0) {
        br.Position = this.Header.NextPObjOffset;

        this.NextPObj = new PObj(this.dat_);
        this.NextPObj.Read(br);
      }
    }

    private void ReadDisplayList_(IBinaryReader br) {
      if (this.Header.DisplayListOffset == 0) {
        return;
      }

      this.VertexSpace = VertexSpace.BONE;

      var weightListOffset = this.Header.WeightListOffset;
      if (weightListOffset != 0) {
        var pObjWeights = this.Weights = new List<IList<PObjWeight>>();

        // Weight list is children of a given bone
        if (!this.Header.Flags.CheckFlag(PObjFlags.OBJTYPE_ENVELOPE)) {
          var currentJObjOffset = weightListOffset;
          while (currentJObjOffset != 0) {
            var jObj = this.dat_.JObjByOffset[currentJObjOffset];
            pObjWeights.Add(new PObjWeight {
                JObj = jObj,
                Weight = 1,
            }.AsList());
            currentJObjOffset = jObj.Data.FirstChildBoneOffset;
          }
        } else {
          br.Position = this.Header.WeightListOffset;
          int offset = 0;
          while ((offset = br.ReadInt32()) != 0) {
            br.SubreadAt(
                offset,
                sbr => {
                  var weights = new List<PObjWeight>();

                  uint jObjOffset;
                  while ((jObjOffset = sbr.ReadUInt32()) != 0) {
                    var weight = sbr.ReadSingle();
                    weights.Add(new PObjWeight {
                        JObj = this.dat_.JObjByOffset[jObjOffset],
                        Weight = weight,
                    });
                  }

                  pObjWeights.Add(weights);
                });
          }
        }
      }

      // Reads display list
      br.Position = this.Header.DisplayListOffset;
      for (var d = 0; d < this.Header.DisplayListSize; ++d) {
        var opcode = (GxOpcode) br.ReadByte();

        // TODO: Is this correct, or should it just skip this instead?
        if (opcode == GxOpcode.NOP) {
          break;
        }

        switch (opcode) {
          case GxOpcode.LOAD_CP_REG: {
            var command = br.ReadByte();
            var value = br.ReadUInt32();

            // TODO: Is this actually needed???
            if (command == 0x50) {
              this.dat_.VertexDescriptorValue &= ~((uint) 0x1FFFF);
              this.dat_.VertexDescriptorValue |= value;
            } else if (command == 0x60) {
              value <<= 17;
              this.dat_.VertexDescriptorValue &= 0x1FFFF;
              this.dat_.VertexDescriptorValue |= value;
            } else {
              throw new NotImplementedException();
            }

            break;
          }
          case GxOpcode.LOAD_XF_REG: {
            var lengthMinusOne = br.ReadUInt16();
            var length = lengthMinusOne + 1;

            // http://hitmen.c02.at/files/yagcd/yagcd/chap5.html#sec5.11.4
            var firstXfRegisterAddress = br.ReadUInt16();

            var values = br.ReadUInt32s(length);
            // TODO: Implement
            break;
          }
          case GxOpcode.DRAW_TRIANGLES:
          case GxOpcode.DRAW_QUADS:
          case GxOpcode.DRAW_TRIANGLE_STRIP: {
            var vertexCount = br.ReadUInt16();
            var vertices = new DatVertex[vertexCount];

            for (var i = 0; i < vertexCount; ++i) {
              int? weightId = null;
              Vector3? position = null;
              Vector3? normal = null;
              Vector3? binormal = null;
              Vector3? tangent = null;
              Vector2? uv0 = null;
              Vector2? uv1 = null;
              IColor? color = null;

              foreach (var vertexDescriptor in this.VertexDescriptors) {
                var vertexAttribute = vertexDescriptor.Attribute;
                var vertexFormat = vertexDescriptor.AttributeType;

                if (vertexAttribute == GxAttribute.CLR0 &&
                    vertexFormat == GxAttributeType.DIRECT) {
                  color = ReadColorAttribute_(
                      br,
                      vertexDescriptor.ColorComponentType);
                  continue;
                }

                if (vertexAttribute == GxAttribute.PNMTXIDX &&
                    vertexFormat == GxAttributeType.DIRECT) {
                  weightId = br.ReadByte() / 3;
                  continue;
                }

                var value = vertexFormat switch {
                    GxAttributeType.DIRECT => br.ReadByte(),
                    GxAttributeType.INDEX_8 => br.ReadByte(),
                    GxAttributeType.INDEX_16 => br.ReadUInt16(),
                    _ => throw new NotImplementedException(),
                };

                var offset = vertexDescriptor.ArrayOffset +
                             vertexDescriptor.Stride * value;

                switch (vertexAttribute) {
                  case GxAttribute.POS: {
                    position = br.SubreadAt(
                        offset,
                        sbr => sbr.ReadVector3(vertexDescriptor));
                    break;
                  }
                  case GxAttribute.NRM: {
                    normal = br.SubreadAt(
                        offset,
                        sbr => Vector3.Normalize(
                            sbr.ReadVector3(vertexDescriptor)));
                    break;
                  }
                  case GxAttribute.NBT: {
                    br.SubreadAt(
                        offset,
                        sbr => {
                          normal = Vector3.Normalize(
                              sbr.ReadVector3(vertexDescriptor));
                          binormal = Vector3.Normalize(
                              sbr.ReadVector3(vertexDescriptor));
                          tangent = Vector3.Normalize(
                              sbr.ReadVector3(vertexDescriptor));
                        });
                    break;
                  }
                  case GxAttribute.TEX0: {
                    uv0 = br.SubreadAt(
                        offset,
                        sbr => sbr.ReadVector2(vertexDescriptor));
                    break;
                  }
                  case GxAttribute.TEX1: {
                    uv1 = br.SubreadAt(
                        offset,
                        sbr => sbr.ReadVector2(vertexDescriptor));
                    break;
                  }
                  default: {
                    break;
                    //throw new NotImplementedException();
                  }
                }
              }

              if (position != null) {
                vertices[i] = new DatVertex {
                    WeightId = weightId,
                    Position = position.Value,
                    Normal = normal,
                    Binormal = binormal,
                    Tangent = tangent,
                    Uv0 = uv0,
                    Uv1 = uv1,
                    Color = color,
                };
              }
            }

            this.Primitives.Add(new DatPrimitive {
                Type = opcode,
                Vertices = vertices
            });
            break;
          }
          default: {
            break;
          }
        }
      }
    }

    private IColor ReadColorAttribute_(IBinaryReader br,
                                       ColorComponentType colorComponentType) {
      switch (colorComponentType) {
        case ColorComponentType.RGB565: {
          return ColorUtil.ParseRgb565(br.ReadUInt16());
        }
        case ColorComponentType.RGB888: {
          return FinColor.FromRgbBytes(
              br.ReadByte(),
              br.ReadByte(),
              br.ReadByte());
        }
        case ColorComponentType.RGBX8888: {
          var color = FinColor.FromRgbBytes(
              br.ReadByte(),
              br.ReadByte(),
              br.ReadByte());
          br.ReadByte();
          return color;
        }
        case ColorComponentType.RGBA4444: {
          ColorUtil.SplitRgba4444(
              br.ReadUInt16(),
              out var r,
              out var g,
              out var b,
              out var a);
          return FinColor.FromRgbaBytes(r, g, b, a);
        }
        case ColorComponentType.RGBA6: {
          var c = br.ReadUInt24();
          var r = ((((c >> 18) & 0x3F) << 2) |
                   (((c >> 18) & 0x3F) >> 4)) / (float) 0xFF;
          var g = ((((c >> 12) & 0x3F) << 2) |
                   (((c >> 12) & 0x3F) >> 4)) / (float) 0xFF;
          var b = ((((c >> 6) & 0x3F) << 2) |
                   (((c >> 6) & 0x3F) >> 4)) / (float) 0xFF;
          var a = ((((c) & 0x3F) << 2) | (((c) & 0x3F) >> 4)) /
                  (float) 0xFF;
          return FinColor.FromRgbaFloats(r, g, b, a);
        }
        case ColorComponentType.RGBA8888: {
          return FinColor.FromRgbaBytes(
              br.ReadByte(),
              br.ReadByte(),
              br.ReadByte(),
              br.ReadByte());
        }
        default: {
          throw new NotImplementedException();
        }
      }
    }
  }

  public static class BinaryReaderExtensions {
    public static Vector2 ReadVector2(this IBinaryReader br,
                                      VertexDescriptor descriptor) {
      var vec2 = new Vector2();
      br.ReadIntoVector(descriptor,
                        new Span<Vector2>(ref vec2).Cast<Vector2, float>());
      return vec2;
    }

    public static Vector3 ReadVector3(this IBinaryReader br,
                                      VertexDescriptor descriptor) {
      var vec3 = new Vector3();
      br.ReadIntoVector(descriptor,
                        new Span<Vector3>(ref vec3).Cast<Vector3, float>());
      return vec3;
    }

    public static Vector4 ReadVector4(this IBinaryReader br,
                                      VertexDescriptor descriptor) {
      var vec4 = new Vector4();
      br.ReadIntoVector(descriptor,
                        new Span<Vector4>(ref vec4).Cast<Vector4, float>());
      return vec4;
    }

    public static void ReadIntoVector(this IBinaryReader br,
                                      VertexDescriptor descriptor,
                                      Span<float> floats) {
      Asserts.True(floats.Length >= descriptor.ComponentCount);

      var scaleMultiplier = 1f / MathF.Pow(2, descriptor.Scale);
      for (var i = 0; i < descriptor.ComponentCount; ++i) {
        floats[i] = scaleMultiplier * descriptor.AxesComponentType switch {
            GxComponentType.U8  => br.ReadByte(),
            GxComponentType.S8  => br.ReadSByte(),
            GxComponentType.U16 => br.ReadUInt16(),
            GxComponentType.S16 => br.ReadInt16(),
            GxComponentType.F32 => br.ReadSingle(),
        };
      }
    }
  }

  public class DatPrimitive {
    public required GxOpcode Type { get; init; }
    public required IReadOnlyList<DatVertex> Vertices { get; init; }
  }

  public class DatVertex {
    public required int? WeightId { get; init; }
    public required Vector3 Position { get; init; }
    public Vector3? Normal { get; init; }
    public Vector3? Binormal { get; init; }
    public Vector3? Tangent { get; init; }
    public Vector2? Uv0 { get; init; }
    public Vector2? Uv1 { get; init; }
    public IColor? Color { get; init; }
  }

  public class PObjWeight {
    public required JObj JObj { get; init; }
    public required float Weight { get; init; }
  }
}