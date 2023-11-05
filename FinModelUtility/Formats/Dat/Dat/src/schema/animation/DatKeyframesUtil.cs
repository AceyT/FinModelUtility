﻿using schema.binary;

namespace dat.schema.animation {
  public static class DatKeyframesUtil {
    /// <summary>
    ///   Shamelessly stolen from:
    ///   https://github.com/Ploaj/HSDLib/blob/93a906444f34951c6eed4d8c6172bba43d4ada98/HSDRaw/Tools/FOBJ_Decoder.cs#L22
    ///   https://github.com/Ploaj/HSDLib/blob/93a906444f34951c6eed4d8c6172bba43d4ada98/HSDRaw/Tools/FOBJ_Player.cs#L162
    /// </summary> 
    public static void ReadKeyframes(
        IBinaryReader br,
        IDatKeyframes datKeyframes,
        LinkedList<(int frame, float incomingValue, float outgoingValue, float?
            incomingTangent, float? outgoingTangent)> keyframes) {
      if (datKeyframes.JointTrackType
          is (< JointTrackType.HSD_A_J_ROTX or > JointTrackType.HSD_A_J_ROTZ)
             and (< JointTrackType.HSD_A_J_TRAX
                  or > JointTrackType.HSD_A_J_TRAZ)
             and (< JointTrackType.HSD_A_J_SCAX
                  or > JointTrackType.HSD_A_J_SCAZ)) {
        return;
      }

      br.PushMemberEndianness(Endianness.LittleEndian);

      var valueScale = (uint) Math.Pow(2, datKeyframes.ValueFlag & 0x1F);
      var tangentScale = (uint) Math.Pow(2, datKeyframes.TangentFlag & 0x1F);

      var valueFormat = (GXAnimDataFormat) (datKeyframes.ValueFlag & 0xE0);
      var tangentFormat = (GXAnimDataFormat) (datKeyframes.TangentFlag & 0xE0);

      keyframes.Clear();
      br.SubreadAt(
          datKeyframes.DataOffset,
          (int) datKeyframes.DataLength,
          sbr => {
            var frame = -datKeyframes.StartFrame;
            while (!sbr.Eof) {
              var type = ReadPacked_(sbr);
              var interpolation = (GxInterpolationType) (type & 0x0F);
              int numOfKey = (type >> 4) + 1;
              if (interpolation == 0) {
                break;
              }

              GxInterpolationType? previousInterpolationType = null;
              float previousValue = 0;
              float? previousTangent = null;

              float value = 0;
              float? tangent = null;

              int frameLength = 0;

              for (int i = 0; i < numOfKey; i++) {
                switch (interpolation) {
                  case GxInterpolationType.Constant:
                    value = ParseFloat_(sbr, valueFormat, valueScale);
                    if (previousInterpolationType != GxInterpolationType.Slp) {
                      tangent = null;
                    }

                    frameLength = ReadPacked_(sbr);
                    break;
                  case GxInterpolationType.Linear:
                    value = ParseFloat_(sbr, valueFormat, valueScale);
                    if (previousInterpolationType != GxInterpolationType.Slp) {
                      tangent = null;
                    }

                    frameLength = ReadPacked_(sbr);
                    break;
                  case GxInterpolationType.Spl0:
                    value = ParseFloat_(sbr, valueFormat, valueScale);
                    tangent = 0;
                    frameLength = ReadPacked_(sbr);
                    break;
                  case GxInterpolationType.Spl:
                    value = ParseFloat_(sbr, valueFormat, valueScale);
                    tangent = ParseFloat_(sbr, tangentFormat, tangentScale);
                    frameLength = ReadPacked_(sbr);
                    break;
                  case GxInterpolationType.Slp:
                    tangent = ParseFloat_(sbr, tangentFormat, tangentScale);
                    frame += frameLength;
                    frameLength = 0;
                    break;
                  case GxInterpolationType.Key:
                    value = ParseFloat_(sbr, valueFormat, valueScale);
                    frame += frameLength;
                    frameLength = 0;
                    break;
                  default:
                    throw new Exception("Unknown Interpolation Type " +
                                        interpolation.ToString("X"));
                }


                float incomingValue = value;
                float outgoingValue = value;
                float? incomingTangent = tangent;
                float? outgoingTangent = tangent;

                // For frames that come after constant/key frames, the incoming value comes from the constant/key frame
                if (previousInterpolationType == GxInterpolationType.Constant ||
                    previousInterpolationType == GxInterpolationType.Key) {
                  incomingValue = previousValue;
                  incomingTangent = previousTangent;
                }

                if (frame >= 0) {
                  keyframes.AddLast((frame, incomingValue, outgoingValue,
                                     incomingTangent, outgoingTangent));
                }

                frame += frameLength;

                previousInterpolationType = interpolation;
                previousValue = value;
                previousTangent = tangent;
              }
            }
          });

      br.PopEndianness();
    }

    /// <summary>
    ///   Shamelessly stolen from:
    ///   https://github.com/Ploaj/HSDLib/blob/master/HSDRaw/BinaryReaderExt.cs#L249C9-L259C10
    /// </summary>
    private static int ReadPacked_(IBinaryReader br) {
      int type = br.ReadByte();
      int i = type;
      if ((i & 0x80) != 0) // max 16 bit I think
      {
        i = br.ReadByte();
        type = (type & 0x7F) | (i << 7);
      }

      return type;
    }

    private static float ParseFloat_(IBinaryReader br,
                                     GXAnimDataFormat format,
                                     float scale)
      => format switch {
          GXAnimDataFormat.Float  => br.ReadSingle(),
          GXAnimDataFormat.Short  => br.ReadInt16() / scale,
          GXAnimDataFormat.UShort => br.ReadUInt16() / scale,
          GXAnimDataFormat.SByte  => br.ReadSByte() / scale,
          GXAnimDataFormat.Byte   => br.ReadByte() / scale,
          _                       => 0
      };
  }
}