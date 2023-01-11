using System.IO;

using fin.schema.vector;

using mod.schema.collision;

using NUnit.Framework;

using schema;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;


namespace mod.schema {
  public class GcnSerializablesTests {
    [Test]
    public void TestBaseCollTriInfo()
      => TestGcnSerializableSimple(new BaseCollTriInfo());

    [Test]
    public void TestBaseRoomInfo()
      => TestGcnSerializableSimple(new BaseRoomInfo());

    [Test]
    public void TestCollGroup() {
      var collGroup = new CollGroup();
      collGroup.unknown1 = new byte[] {1, 2, 3, 4};

      TestGcnSerializableExisting(collGroup);
    }

    [Test]
    public void TestCollGrid() {
      var collGrid = new CollGrid();

      collGrid.boundsMin.Set(1, 2, 3);
      collGrid.boundsMax.Set(4, 5, 6);
      collGrid.unknown1 = 7;
      collGrid.gridX = 2;
      collGrid.gridY = 3;
      for (var i = 0; i < 2 * 3; ++i) {
        collGrid.unknown2.Add(i);
      }

      var collGroup = new CollGroup();
      collGroup.unknown1 = new byte[] {1, 2, 3, 4};

      collGrid.groups.Add(collGroup);
      collGrid.groups.Add(collGroup);

      TestGcnSerializableExisting(collGrid);
    }

    /*[Test]
    public void TestEnvelope() => TestGcnSerializableSimple(new Envelope());*/

    [Test]
    public void TestJoint() {
      var joint = new Joint();

      joint.parentIdx = 1;
      joint.flags = 2;
      joint.boundsMin.Set(3.1f, 4.1f, 5.1f);
      joint.boundsMax.Set(6.1f, 7.1f, 8.1f);
      joint.volumeRadius = 9.1f;
      joint.scale.Set(10.1f, 11.1f, 12.1f);
      joint.rotation.Set(13.1f, 14.1f, 15.1f);
      joint.position.Set(16.1f, 17.1f, 18.1f);
      joint.matpolys = new[] {
          new JointMatPoly {
              matIdx = 19,
              meshIdx = 20,
          },
          new JointMatPoly {
              matIdx = 20,
              meshIdx = 21,
          },
      };

      TestGcnSerializableExisting(joint);
    }

    [Test]
    public void TestJointMatPoly()
      => TestGcnSerializableSimple(new JointMatPoly());

    [Test]
    public void TestMesh() {
      var mesh = new Mesh {
          boneIndex = 1,
          vtxDescriptor = 2
      };

      var meshPacket = new MeshPacket();
      meshPacket.indices = new short[] {3, 4};

      var displayList = new DisplayList();
      displayList.flags.intView = 5;
      displayList.cmdCount = 6;
      displayList.dlistData = new byte[] {7, 8};
      meshPacket.displaylists = new[] {displayList, displayList};

      mesh.packets = new[] {meshPacket, meshPacket};

      TestGcnSerializableExisting(mesh);
    }

    [Test]
    public void TestNbt() => TestGcnSerializableSimple(new Nbt());

    [Test]
    public void TestPlane() => TestGcnSerializableSimple(new Plane());

    [Test]
    public void TestTexture() {
      var texture = new Texture {
          width = 1,
          height = 2,
          format = (Texture.TextureFormat) 3,
      };
      texture.imageData = new byte[] {5, 6};

      TestGcnSerializableExisting(texture);
    }

    [Test]
    public void TestTextureAttributes() {
      var textureAttributes = new TextureAttributes {
          index = 1,
          TilingModeS = (TilingMode) 2,
          TilingModeT = (TilingMode) 3,
          unknown1 = 4,
          unknown2 = 5
      };

      TestGcnSerializableExisting(textureAttributes);
    }

    [Test]
    public void TestVector2i() => TestGcnSerializableSimple(new Vector2i());


    [Test]
    public void TestVector3f() => TestGcnSerializableSimple(new Vector3f());

    [Test]
    public void TestVector3i() => TestGcnSerializableSimple(new Vector3i());

    [Test]
    public void TestVtxMatrix() => TestGcnSerializableSimple(new VtxMatrix());

    public static async void TestGcnSerializableSimple(
        IBiSerializable gcnSerializable) {
      var dataLen = 100;
      var inData = new byte[dataLen];
      for (var i = 0; i < dataLen; ++i) {
        inData[i] = (byte) i;
      }

      using var reader =
          new EndianBinaryReader(new MemoryStream(inData),
                                 Endianness.BigEndian);
      gcnSerializable.Read(reader);

      var writer = new EndianBinaryWriter(Endianness.BigEndian);
      gcnSerializable.Write(writer);

      var outData = new byte[dataLen];
      using var outStream = new MemoryStream(outData);
      await writer.CompleteAndCopyToDelayed(outStream);

      Assert.AreEqual(reader.Position,
                      outStream.Position,
                      "Expected reader and writer to move the same amount.");
      for (var i = 0; i < reader.Position; ++i) {
        Assert.AreEqual(inData[i],
                        outData[i],
                        $"Expected data to be equal at index: {i}");
      }
    }

    public static async void TestGcnSerializableExisting(
        IBiSerializable gcnSerializable) {
      var dataLen = 300;
      var firstWriter = new EndianBinaryWriter(Endianness.BigEndian);
      gcnSerializable.Write(firstWriter);

      var firstOutData = new byte[dataLen];
      using var firstOutStream = new MemoryStream(firstOutData);
      await firstWriter.CompleteAndCopyToDelayed(firstOutStream);

      using var reader =
          new EndianBinaryReader(firstOutData, Endianness.BigEndian);
      gcnSerializable.Read(reader);

      var secondWriter =
          new EndianBinaryWriter(Endianness.BigEndian);
      gcnSerializable.Write(secondWriter);

      var secondOutData = new byte[dataLen];
      using var secondOutStream = new MemoryStream(secondOutData);
      await secondWriter.CompleteAndCopyToDelayed(secondOutStream);

      Assert.IsTrue(firstOutStream.Position == reader.Position &&
                    reader.Position == secondOutStream.Position,
                    "Expected all readers & writers to move the same amount.");
      for (var i = 0; i < reader.Position; ++i) {
        Assert.AreEqual(firstOutData[i],
                        secondOutData[i],
                        $"Expected data to be equal at index: {i}");
      }
    }
  }
}