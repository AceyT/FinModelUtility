﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using f3dzex2.io;

using fin.io;
using fin.schema;

using schema.binary;

namespace UoT.memory {
  public class ZSegments {
    public static ZSegments Instance { get; private set; }

    public IReadOnlyList<ZObject> Objects { get; }
    public IReadOnlyList<ZCodeFiles> ActorCode { get; }
    public IReadOnlyList<ZScene> Scenes { get; }
    public IReadOnlyList<ZOtherData> Others { get; }

    private ZSegments(
        IList<ZObject> objects,
        IList<ZCodeFiles> actorCode,
        IList<ZScene> levels,
        IList<ZOtherData> others) {
      this.Objects = new ReadOnlyCollection<ZObject>(objects);
      this.ActorCode = new ReadOnlyCollection<ZCodeFiles>(actorCode);
      this.Scenes = new ReadOnlyCollection<ZScene>(levels);
      this.Others = new ReadOnlyCollection<ZOtherData>(others);
    }

    public static ZSegments InitializeFromFile(IGenericFile romFile) {
      using var er =
          new EndianBinaryReader(romFile.OpenRead(), Endianness.BigEndian);

      for (long i = 0; i < er.Length; i += 16) {
        var romId = er.Subread(i, ser => ser.ReadString(6));
        if (romId != "zelda@") {
          continue;
        }

        i += 0xd;

        er.Position = i;
        while ((er.ReadByte() >> 4) != 3) {
          ;
        }

        i = (er.Position -= 1);

        var buildDate = er.Subread(i, ser => ser.ReadString(17));
        var segmentOffset = (int) (i + 0x20);

        int nameOffset;

        switch (buildDate) {
          case "00-07-31 17:04:16": {
            nameOffset = -1;
            break;
          }
          case "03-02-21 00:16:31": {
            nameOffset = 0xBE80;
            break;
          }
          default: throw new NotSupportedException();
        }

        return ZSegments.InitializeFromEndianBinaryReader(er, segmentOffset, nameOffset);
      }

      throw new NotSupportedException();
    }

    public static ZSegments InitializeFromEndianBinaryReader(
        IEndianBinaryReader er,
        int segmentOffset,
        int nameOffset) {
      var zSegments = ZSegments.GetZSegments_(er, segmentOffset, nameOffset);

      var objects = new List<ZObject>();
      var actorCode = new List<ZCodeFiles>();
      var scenes = new LinkedList<ZScene>();
      var others = new List<ZOtherData>();

      foreach (var zSegment in zSegments) {
        var fileName = zSegment.FileName;
        var segment = zSegment.Segment;

        BZFile file;
        if (fileName.StartsWith("object_")) {
          var obj = new ZObject(segment);
          file = obj;

          objects.Add(obj);
        } else if (fileName.StartsWith("ovl_")) {
          var ovl = new ZCodeFiles(segment);
          file = ovl;

          actorCode.Add(ovl);
        } else if (fileName.EndsWith("_scene")) {
          var scene = new ZScene(segment);
          file = scene;

          scenes.AddLast(scene);
        } else if (fileName.Contains("_room")) {
          var scene = scenes.Last.Value;

          var map = new ZMap(segment) { Scene = scene };
          file = map;

          var mapCount = scene.Maps?.Length ?? 0;
          Array.Resize(ref scene.Maps, mapCount + 1);
          scene.Maps[mapCount] = map;
        } else {
          var other = new ZOtherData(segment);
          file = other;

          others.Add(other);
        }

        file.FileName = fileName;
      }

      return Instance = new ZSegments(objects, actorCode, scenes.ToArray(), others);
    }

    [Unknown]
    private static IEnumerable<ZSegment> GetZSegments_(
        IEndianBinaryReader er,
        int segmentOffset,
        long nameOffset) {
      var segments = new LinkedList<ZSegment>();

      er.Subread(
          segmentOffset,
          ser => {
            while (true) {
              var startAddress = ser.ReadUInt32();
              var endAddress = ser.ReadUInt32();

              if (startAddress == 0 && endAddress == 0) {
                break;
              }

              var unk0 = ser.ReadUInt32();
              var unk1 = ser.ReadUInt32();

              var fileNameBuilder = new StringBuilder();
              var pos = ser.Position;
              ser.Position = nameOffset;
              var inName = false;
              while (true) {
                var c = ser.ReadChar();

                if (c == '\0') {
                  if (inName) {
                    break;
                  }
                } else {
                  inName = true;
                  fileNameBuilder.Append(c);
                }
              }
              nameOffset = ser.Position;
              ser.Position = pos;

              var fileName = fileNameBuilder.ToString();

              segments.AddLast(new ZSegment {
                  FileName = fileName,
                  Segment = new Segment {
                    Offset = startAddress,
                    Length = endAddress - startAddress,
                  }
              });
            }
          });

      return segments;
    }

    private class ZSegment {
      public required string FileName { get; init; }
      public required Segment Segment { get; init; }
    }
  }
}