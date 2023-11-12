﻿using System.Collections;

using fin.data.counters;
using fin.data.dictionaries;
using fin.data.queues;
using fin.io;
using fin.model;

using schema.binary;

namespace visceral.api {
  public class BnkReader {
    public enum MaybeBoneType {
      ROOT = 0x2,
      PARENT = 0x5,
      ANIMATED = 0x16,
    }

    public enum AxisType : byte {
      ROT_X,
      ROT_Y,
      ROT_Z,
      ROT_W,
      POS_X,
      POS_Y,
      POS_Z,
    }

    public enum KeyframeType : byte {
      ONLY_KEYFRAME = 0x0,
      KEYFRAME_AND_3_BYTES = 0x1,
      KEYFRAME_AND_6_BYTES = 0x2,
      KEYFRAME_AND_9_BYTES = 0x3,
      FLOATS = 0x4,
      BYTE_GRADIENT = 0x6,
      SHORT_GRADIENT = 0x7,
      SINGLETON_0 = 0xC,
      SINGLETON_1 = 0xD,
    }

    public void ReadBnk(IModel model,
                        IReadOnlyGenericFile bnkFile,
                        IReadOnlyGenericFile? rcbFile,
                        IBone[] bones) {
      using var bnkBr =
          new SchemaBinaryReader(bnkFile.OpenRead(), Endianness.LittleEndian);

      bnkBr.Position = 0x24;
      var headerCount = bnkBr.ReadUInt32();
      var headerOffsetOffset = bnkBr.ReadUInt32();

      bnkBr.Position = headerOffsetOffset;
      var headerOffsets = bnkBr.ReadUInt32s(headerCount);

      foreach (var headerOffset in headerOffsets) {
        var finAnimation = model.AnimationManager.AddAnimation();
        finAnimation.FrameRate = 60;

        bnkBr.Position = headerOffset;

        if (GeoModelImporter.STRICT_DAT) {
          this.ReadIntoAnimation_(bnkBr, rcbFile, bones, finAnimation);
        } else {
          try {
            this.ReadIntoAnimation_(bnkBr, rcbFile, bones, finAnimation);
          } catch (Exception e) {
            model.AnimationManager.RemoveAnimation(finAnimation);
          }
        }
      }
    }

    private void ReadIntoAnimation_(IBinaryReader bnkBr,
                                    IReadOnlyGenericFile? rcbFile,
                                    IBone[] bones,
                                    IModelAnimation finAnimation) {
      {
        var animationNameOffset = bnkBr.ReadUInt32();
        finAnimation.Name =
            bnkBr.SubreadAt(animationNameOffset, ser => ser.ReadStringNT());
      }

      var totalFrames = 1;

      var commands = new List<ushort>();
      var countsOfAxesWithKeyframeType =
          new CounterSet<(AxisType, KeyframeType)>();
      var valuesByAxesWithKeyframeType =
          new SortedSetDictionary<(AxisType, KeyframeType), float>();

      {
        var rootOffset = bnkBr.ReadUInt32();
        var maybeBoneOffsetQueue = new FinQueue<uint>(rootOffset);
        while (maybeBoneOffsetQueue.TryDequeue(out var maybeBoneOffset)) {
          bnkBr.Position = maybeBoneOffset;

          var boneType = (MaybeBoneType) bnkBr.ReadByte();
          var childCount = bnkBr.ReadByte();
          var maybeId = bnkBr.ReadByte();
          var unk1 = bnkBr.ReadByte();

          var someMetadataOffset = bnkBr.ReadUInt32();
          var unk2 = bnkBr.ReadUInt32();

          if (childCount > 0) {
            maybeBoneOffsetQueue.Enqueue(bnkBr.ReadUInt32s(childCount));
          }

          if (boneType == MaybeBoneType.ANIMATED) {
            var animationDataOffset = bnkBr.ReadUInt32();
            bnkBr.Position = animationDataOffset;

            var unkHash = bnkBr.ReadUInt32();
            var maybeVersion = bnkBr.ReadUInt32();
            var unk3 = bnkBr.ReadUInt32();
            var unk4 = bnkBr.ReadUInt32();
            var unk5 = bnkBr.ReadUInt32();

            var someHashFromRcb = bnkBr.ReadUInt32();
            var standaloneCommandPrefix = bnkBr.ReadUInt32();

            // These are unknown
            bnkBr.Position += 4 * 5;

            using var rcbEr =
                new SchemaBinaryReader(rcbFile.OpenRead(),
                                       Endianness.LittleEndian);
            rcbEr.Position = 0x24;
            var someDefinitionsOffset = rcbEr.ReadUInt32();
            rcbEr.Position = someDefinitionsOffset;
            while (rcbEr.ReadUInt32() != someHashFromRcb) ;
            var bitMaskOffsetOffset = rcbEr.ReadUInt32();
            rcbEr.Position = bitMaskOffsetOffset;
            var bitMaskOffset = rcbEr.ReadUInt32();
            var bitMaskLength = rcbEr.ReadUInt32();

            rcbEr.Position = bitMaskOffset;
            var bitMaskArray =
                new BitArray(
                    rcbEr.ReadBytes(
                        (int) Math.Ceiling(bitMaskLength / 8f)));

            for (var b = 0; b < bitMaskLength; ++b) {
              var isActive = bitMaskArray[b];
              if (!isActive) {
                continue;
              }

              var boneTracks = finAnimation.AddBoneTracks(bones[b]);
              var rotations = boneTracks.UseQuaternionAxesRotationTrack();
              var positions = boneTracks.UseSeparatePositionAxesTrack();

              for (var a = 0; a < 7; ++a) {
                var axisType = (AxisType) a;

                void SetKeyframe(int frame, float value) {
                  totalFrames = Math.Max(totalFrames, frame);

                  switch (axisType) {
                    case AxisType.ROT_X:
                    case AxisType.ROT_Y:
                    case AxisType.ROT_Z:
                    case AxisType.ROT_W: {
                      rotations.Set(frame, axisType - AxisType.ROT_X, value);
                      break;
                    }
                    case AxisType.POS_X:
                    case AxisType.POS_Y:
                    case AxisType.POS_Z: {
                      positions.Set(frame, axisType - AxisType.POS_X, value);
                      break;
                    }
                    default: throw new Exception();
                  }
                }

                var command = bnkBr.ReadUInt16();
                commands?.Add(command);

                var upper = command >> 4;
                var lower = command & 0xF;

                if (upper == standaloneCommandPrefix) {
                  var keyframeType = (KeyframeType) lower;

                  countsOfAxesWithKeyframeType.Increment(
                      (axisType, keyframeType));

                  bnkBr.Position -= 1;

                  var frame = 0;
                  foreach (var keyframeValue in
                           this.ReadKeyframeValuesOfType_(
                               bnkBr,
                               keyframeType,
                               (int) standaloneCommandPrefix)) {
                    SetKeyframe(frame++, keyframeValue);
                    valuesByAxesWithKeyframeType.Add(
                        (axisType, keyframeType),
                        keyframeValue);
                  }
                } else if (lower == 5) {
                  var keyframeCount = upper;

                  var frame = 0;
                  for (var k = 0; k < keyframeCount; ++k) {
                    var lengthAndKeyframeType = bnkBr.ReadUInt16();
                    commands?.Add(lengthAndKeyframeType);

                    var keyframeLength = lengthAndKeyframeType >> 4;
                    var keyframeType =
                        (KeyframeType) (lengthAndKeyframeType & 0xF);

                    countsOfAxesWithKeyframeType.Increment(
                        (axisType, keyframeType));

                    bnkBr.Position -= 1;

                    var startingKeyframe = frame;
                    foreach (var keyframeValue in
                             this.ReadKeyframeValuesOfType_(
                                 bnkBr,
                                 keyframeType,
                                 keyframeLength)) {
                      SetKeyframe(frame++, keyframeValue);
                      valuesByAxesWithKeyframeType.Add(
                          (axisType, keyframeType),
                          keyframeValue);
                    }

                    frame = startingKeyframe + keyframeLength;
                  }
                } else {
                  throw new NotImplementedException();
                }
              }
            }
          }
        }
      }

      finAnimation.FrameCount = totalFrames;
    }

    private IEnumerable<float> ReadKeyframeValuesOfType_(
        IBinaryReader br,
        KeyframeType keyframeType,
        int keyframeLength) {
      switch (keyframeType) {
        case KeyframeType.SINGLETON_0: {
          br.Position += 1;
          yield return 0;
          break;
        }
        case KeyframeType.SINGLETON_1: {
          br.Position += 1;
          yield return 1;
          break;
        }
        case KeyframeType.ONLY_KEYFRAME:
        case KeyframeType.KEYFRAME_AND_3_BYTES:
        case KeyframeType.KEYFRAME_AND_6_BYTES:
        case KeyframeType.KEYFRAME_AND_9_BYTES: {
          // TODO: What are these???
          var values = br.ReadBytes(3 * (int) keyframeType);
          yield return br.ReadSingle();
          break;
        }
        case KeyframeType.FLOATS: {
          br.Position += 1;

          for (var i = 0; i < keyframeLength; ++i) {
            yield return br.ReadSingle();
          }

          break;
        }
        case KeyframeType.BYTE_GRADIENT:
        case KeyframeType.SHORT_GRADIENT: {
          br.Position += 1;

          var unk0 = br.ReadByte();
          var unk1 = br.ReadByte();

          br.Position -= 2;

          // TODO: What is this meant to be?
          var unk = br.ReadUInt16();

          var scale = br.ReadSingle();

          // TODO: Is this right????
          var values =
              (keyframeType == KeyframeType.BYTE_GRADIENT
                  ? br.ReadBytes(keyframeLength)
                      .Select(u => (ushort) u)
                  : br.ReadUInt16s(keyframeLength)
                      .Select(u => u))
              .ToArray();

          var keyframes =
              values
                  .Select(value => value * scale)
                  .ToArray();

          if (unk0 == unk1 && unk0 != 0) {
            ;
          }

          foreach (var keyframe in keyframes) {
            yield return keyframe;
          }

          break;
        }
        default: throw new NotImplementedException();
      }
    }
  }
}