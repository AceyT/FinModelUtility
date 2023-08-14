﻿using System.Collections.Generic;
using System.Numerics;

using fin.math;
using fin.model;

using SharpGLTF.Schema2;

namespace fin.exporter.gltf {
  using GltfNode = Node;

  public class GltfAnimationBuilder {
    public void BuildAnimations(
        ModelRoot gltfModel,
        (GltfNode, IBone)[] skinNodesAndBones,
        float modelScale,
        IReadOnlyList<IModelAnimation> animations) {
      foreach (var animation in animations) {
        var gltfAnimation = gltfModel.UseAnimation(animation.Name);

        var fps = animation.FrameRate;

        // Writes translation/rotation/scale for each joint.
        var translationKeyframes = new Dictionary<float, Vector3>();
        var rotationKeyframes = new Dictionary<float, Quaternion>();
        var scaleKeyframes = new Dictionary<float, Vector3>();
        foreach (var (node, bone) in skinNodesAndBones) {
          if (!animation.BoneTracks.TryGetValue(bone, out var boneTracks)) {
            continue;
          }

          translationKeyframes.Clear();
          rotationKeyframes.Clear();
          scaleKeyframes.Clear();

          var translationDefined = boneTracks.Positions?.IsDefined ?? false;
          var rotationDefined = boneTracks.Rotations?.IsDefined ?? false;
          var scaleDefined = boneTracks.Scales?.IsDefined ?? false;

          // TODO: How to get keyframes for sparse tracks?
          for (var i = 0; i < animation.FrameCount; ++i) {
            var time = i / fps;

            if (translationDefined) {
              if (boneTracks.Positions.TryGetInterpolatedFrame(
                      i,
                      out var position)) {
                translationKeyframes[time] =
                    new Vector3(position.X * modelScale,
                                position.Y * modelScale,
                                position.Z * modelScale);
              }
            }

            if (rotationDefined) {
              if (boneTracks.Rotations.TryGetInterpolatedFrame(
                      i,
                      out var rotation)) {
                rotationKeyframes[time] = rotation;
              }
            }

            if (scaleDefined) {
              var scale = boneTracks.Scales.GetInterpolatedFrame(i);
              scaleKeyframes[time] = new Vector3(scale.X, scale.Y, scale.Z);
            }
          }

          if (translationDefined) {
            gltfAnimation.CreateTranslationChannel(
                node,
                translationKeyframes);
          }

          if (rotationDefined) {
            gltfAnimation.CreateRotationChannel(
                node,
                rotationKeyframes);
          }

          if (scaleDefined) {
            gltfAnimation.CreateScaleChannel(
                node,
                scaleKeyframes);
          }
        }
      }
    }
  }
}