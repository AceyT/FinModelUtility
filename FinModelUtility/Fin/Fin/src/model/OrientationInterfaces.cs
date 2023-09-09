﻿using System;
using System.Numerics;

namespace fin.model {
  public readonly struct Position {
    public Position() : this(0, 0, 0) { }

    public Position(float x, float y, float z) {
      X = x;
      Y = y;
      Z = z;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }


    public override string ToString() =>
        $"{{{this.X}, {this.Y}, {this.Z}}}";

    public override bool Equals(object? other) {
      if (other is Position otherPosition) {
        var error = .0001;
        return Math.Abs(this.X - otherPosition.X) < error &&
               Math.Abs(this.Y - otherPosition.Y) < error &&
               Math.Abs(this.Z - otherPosition.Z) < error;
      }

      return false;
    }
  }

  public readonly struct Scale {
    public Scale() : this(0, 0, 0) { }

    public Scale(float scale) : this(scale, scale, scale) { }

    public Scale(float x, float y, float z) {
      X = x;
      Y = y;
      Z = z;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public override string ToString() =>
        $"{{{this.X}, {this.Y}, {this.Z}}}";

    public override bool Equals(object? other) {
      if (other is Scale otherScale) {
        var error = .0001;
        return Math.Abs(this.X - otherScale.X) < error &&
               Math.Abs(this.Y - otherScale.Y) < error &&
               Math.Abs(this.Z - otherScale.Z) < error;
      }

      return false;
    }
  }

  public readonly struct Normal {
    public Normal() : this(0, 0, 0) { }

    public Normal(float x, float y, float z) {
      X = x;
      Y = y;
      Z = z;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public override string ToString() =>
        $"{{{this.X}, {this.Y}, {this.Z}}}";

    public override bool Equals(object? other) {
      if (other is Normal otherNormal) {
        var error = .0001;
        return Math.Abs(this.X - otherNormal.X) < error &&
               Math.Abs(this.Y - otherNormal.Y) < error &&
               Math.Abs(this.Z - otherNormal.Z) < error;
      }

      return false;
    }
  }

  public readonly struct Tangent {
    public Tangent() : this(0, 0, 0, 0) { }

    public Tangent(float x, float y, float z, float w) {
      X = x;
      Y = y;
      Z = z;
      W = w;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float W { get; }


    public override string ToString() =>
        $"{{{this.X}, {this.Y}, {this.Z}, {this.W}}}";

    public override bool Equals(object? other) {
      if (other is Tangent otherTangent) {
        var error = .0001;
        return Math.Abs(this.X - otherTangent.X) < error &&
               Math.Abs(this.Y - otherTangent.Y) < error &&
               Math.Abs(this.Z - otherTangent.Z) < error &&
               Math.Abs(this.W - otherTangent.W) < error;
      }

      return false;
    }
  }


  public interface IVector2 {
    float X { get; set; }
    float Y { get; set; }
  }


  public interface IReadOnlyVector3 {
    float X { get; }
    float Y { get; }
    float Z { get; }
  }

  public interface IVector3 : IReadOnlyVector3 {
    float IReadOnlyVector3.X => this.X;
    new float X { get; set; }

    float IReadOnlyVector3.Y => this.Y;
    new float Y { get; set; }
    
    float IReadOnlyVector3.Z => this.Z;
    new float Z { get; set; }
  }

  public interface IVector4 {
    float X { get; set; }
    float Y { get; set; }
    float Z { get; set; }
    float W { get; set; }
  }


  /*public interface IQuaternion {
    float X { get; }
    float Y { get; }
    float Z { get; }
    float W { get; }

    float Length { get; }

    float XDegrees { get; }
    float YDegrees { get; }
    float ZDegrees { get; }
    IQuaternion SetDegrees(float x, float y, float z);

    float XRadians { get; }
    float YRadians { get; }
    float ZRadians { get; }
    IQuaternion SetRadians(float x, float y, float z);
  }*/

  public interface IRotation {
    float XDegrees { get; }
    float YDegrees { get; }
    float ZDegrees { get; }
    IRotation SetDegrees(float x, float y, float z);

    float XRadians { get; }
    float YRadians { get; }
    float ZRadians { get; }
    IRotation SetRadians(float x, float y, float z);

    IRotation SetQuaternion(Quaternion q);
  }
}