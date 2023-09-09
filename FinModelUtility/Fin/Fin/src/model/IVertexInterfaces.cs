﻿using System.Drawing;
using System.Numerics;

using fin.color;
using fin.data;

namespace fin.model {
  public interface IReadOnlyVertex : IIndexable {
    IBoneWeights? BoneWeights { get; }
    Position LocalPosition { get; }
  }

  public interface IVertex : IReadOnlyVertex {
    void SetBoneWeights(IBoneWeights boneWeights);

    void SetLocalPosition(Position localPosition);
    void SetLocalPosition(Vector3 localPosition);
    void SetLocalPosition(IReadOnlyVector3 localPosition);
    void SetLocalPosition(float x, float y, float z);
  }


  public interface IReadOnlyNormalVertex : IReadOnlyVertex {
    Normal? LocalNormal { get; }
  }

  public interface INormalVertex : IReadOnlyNormalVertex, IVertex {
    void SetLocalNormal(Normal? localNormal);
    void SetLocalNormal(Vector3? localNormal);
    void SetLocalNormal(IReadOnlyVector3? localNormal);
    void SetLocalNormal(float x, float y, float z);
  }


  public interface IReadOnlyTangentVertex : IReadOnlyVertex {
    Tangent? LocalTangent { get; }
  }

  public interface ITangentVertex : IReadOnlyTangentVertex, IVertex {
    void SetLocalTangent(Tangent? localTangent);
    void SetLocalTangent(Vector4? localTangent);
    void SetLocalTangent(IReadOnlyVector4? localTangent);
    void SetLocalTangent(float x, float y, float z, float w);
  }


  public interface IReadOnlyNormalTangentVertex : IReadOnlyNormalVertex,
                                                  IReadOnlyTangentVertex { }

  public interface INormalTangentVertex : IReadOnlyNormalTangentVertex,
                                          INormalVertex, ITangentVertex { }


  public interface IReadOnlySingleColorVertex : IReadOnlyVertex {
    IColor? GetColor();
  }

  public interface ISingleColorVertex : IReadOnlySingleColorVertex, IVertex {
    void SetColor(Color? color);
    void SetColor(IColor? color);
    void SetColor(Vector4? color);
    void SetColor(IReadOnlyVector4? color);
    void SetColorBytes(byte r, byte g, byte b, byte a);
  }


  public interface IReadOnlyMultiColorVertex : IReadOnlyVertex {
    int ColorCount { get; }
    IColor? GetColor(int colorIndex);
  }

  public interface IMultiColorVertex : IReadOnlyMultiColorVertex, IVertex {
    void SetColor(int colorIndex, IColor? color);

    void SetColorBytes(int colorIndex,
                       byte r,
                       byte g,
                       byte b,
                       byte a);
  }


  public interface IReadOnlySingleUvVertex : IReadOnlyVertex {
    TexCoord? GetUv();
  }

  public interface ISingleUvVertex : IReadOnlySingleUvVertex, IVertex {
    void SetUv(TexCoord? uv);
    void SetUv(Vector2? uv);
    void SetUv(IReadOnlyVector2? uv);
    void SetUv(float u, float v);
  }


  public interface IReadOnlyMultiUvVertex : IReadOnlyVertex {
    int UvCount { get; }
    TexCoord? GetUv(int uvIndex);
  }

  public interface IMultiUvVertex : IReadOnlyMultiUvVertex, IVertex {
    void SetUv(int uvIndex, TexCoord? uv);
    void SetUv(int uvIndex, float u, float v);
  }
}