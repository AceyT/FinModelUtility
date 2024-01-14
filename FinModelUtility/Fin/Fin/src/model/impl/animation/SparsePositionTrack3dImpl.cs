﻿using System;
using System.Runtime.CompilerServices;

using fin.animation;
using fin.math.interpolation;

namespace fin.model.impl {
  public partial class ModelImpl<TVertex> {
    public class SeparatePositionAxesTrack3dImpl
        : BScalarAxesTrack<Position, float, FloatInterpolator>,
          ISeparatePositionAxesTrack3d {
      private readonly IBone bone_;

      public SeparatePositionAxesTrack3dImpl(IAnimation animation,
                                             IBone bone,
                                             ReadOnlySpan<int>
                                                 initialCapacityPerAxis) :
          base(animation, 3, initialCapacityPerAxis, new FloatInterpolator()) {
        this.bone_ = bone;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public override bool TryGetInterpolatedFrame(
          float frame,
          out Position interpolatedValue,
          AnimationInterpolationConfig? config = null) {
        var localPosition = this.bone_.LocalPosition;

        if (!this.axisTracks[0].TryGetInterpolatedFrame(frame, out var x, config)) {
          x = localPosition.X;
        }

        if (!this.axisTracks[1].TryGetInterpolatedFrame(frame, out var y, config)) {
          y = localPosition.Y;
        }

        if (!this.axisTracks[2].TryGetInterpolatedFrame(frame, out var z, config)) {
          z = localPosition.Z;
        }

        interpolatedValue = new(x, y, z);
        return true;
      }
    }
  }
}