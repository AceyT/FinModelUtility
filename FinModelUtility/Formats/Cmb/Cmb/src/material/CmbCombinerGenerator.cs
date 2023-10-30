﻿using System;
using System.Collections.Generic;
using System.Linq;

using cmb.schema.cmb;
using cmb.schema.cmb.mats;

using fin.language.equations.fixedFunction;
using fin.language.equations.fixedFunction.impl;
using fin.model;
using fin.schema.color;
using fin.util.enumerables;

using Material = cmb.schema.cmb.mats.Material;

namespace cmb.material {
  /// <summary>
  ///   Shamelessly copied from https://github.com/naclomi/noclip.website/blob/8b0de601d6d8f596683f0bdee61a9681a42512f9/src/oot3d/render.ts
  /// </summary>
  public class CmbCombinerGenerator {
    private readonly Material cmbMaterial_;
    private readonly IFixedFunctionEquations<FixedFunctionSource> equations_;
    private readonly IFixedFunctionRegisters registers_;
    private readonly ColorFixedFunctionOps cOps_;
    private readonly ScalarFixedFunctionOps sOps_;

    private int constColorIndex_;
    private Rgba32 constColor_;

    private IColorValue? previousColor_;
    private IColorValue? previousColorBuffer_;
    private IScalarValue? previousAlpha_;
    private IScalarValue? previousAlphaBuffer_;

    private IColorValue? lightColor_;
    private IScalarValue? lightAlpha_;

    public CmbCombinerGenerator(Material cmbMaterial,
                                IFixedFunctionMaterial finMaterial) {
      this.cmbMaterial_ = cmbMaterial;
      this.equations_ = finMaterial.Equations;
      this.registers_ = finMaterial.Registers;
      this.cOps_ = new ColorFixedFunctionOps(this.equations_);
      this.sOps_ = new ScalarFixedFunctionOps(this.equations_);

      var bufferColor = cmbMaterial.bufferColor;
      this.previousColorBuffer_ =
          new ColorConstant(bufferColor[0], bufferColor[1], bufferColor[2]);
      this.previousAlpha_ = new ScalarConstant(bufferColor[3]);
    }

    public void AddCombiners(IReadOnlyList<Combiner?> cmbCombiners) {
      var dependsOnLights =
          this.cmbMaterial_.isHemiSphereLightingEnabled &&
          cmbCombiners
              .Nonnull()
              .SelectMany(combiner
                              => combiner.colorSources.Concat(
                                  combiner.alphaSources))
              .Any(source => source is TexCombinerSource.FragmentPrimaryColor
                                       or TexCombinerSource
                                           .FragmentSecondaryColor);

      if (!dependsOnLights) {
        this.lightColor_ = this.equations_.CreateColorConstant(1);
        this.lightAlpha_ = this.equations_.CreateScalarConstant(1);
      } else {
        // TODO: Is this lighting calculation right??
        var ambientRgba = this.cmbMaterial_.ambientColor;
        var ambientColor =
            new ColorConstant(ambientRgba.Rf, ambientRgba.Gf, ambientRgba.Bf);
        var ambientAlpha = new ScalarConstant(ambientRgba.Af);

        var diffuseRgba = this.cmbMaterial_.diffuseRgba;
        var diffuseColor =
            new ColorConstant(diffuseRgba.Rf, diffuseRgba.Gf, diffuseRgba.Bf);
        var diffuseAlpha = new ScalarConstant(diffuseRgba.Af);

        var specularRgba = this.cmbMaterial_.specular0Color;
        var specularColor =
            new ColorConstant(specularRgba.Rf,
                              specularRgba.Gf,
                              specularRgba.Bf);
        var specularAlpha = new ScalarConstant(specularRgba.Af);

        this.lightColor_ =
            this.cOps_.Add(
                this.cOps_.Add(
                    this.cOps_.Multiply(
                        this.equations_.CreateOrGetColorInput(
                            FixedFunctionSource.LIGHT_AMBIENT_COLOR),
                        ambientColor),
                    this.cOps_.Multiply(
                        this.equations_.GetMergedLightDiffuseColor(),
                        diffuseColor)),
                this.cOps_.Multiply(
                    this.equations_.GetMergedLightSpecularColor(),
                    specularColor));
        this.lightAlpha_ =
            this.sOps_.Add(
                this.sOps_.Add(
                    this.sOps_.Multiply(
                        this.equations_.CreateOrGetScalarInput(
                            FixedFunctionSource.LIGHT_AMBIENT_ALPHA),
                        ambientAlpha),
                    this.sOps_.Multiply(
                        this.equations_.GetMergedLightDiffuseAlpha(),
                        diffuseAlpha)),
                this.sOps_.Multiply(
                    this.equations_.GetMergedLightSpecularAlpha(),
                    specularAlpha));
      }

      foreach (var cmbCombiner in cmbCombiners) {
        if (cmbCombiner == null) {
          continue;
        }

        this.AddCombiner_(cmbCombiner);
      }

      // TODO: This doesn't seem right, it seems like this should be light attributes instead??
      // Applies diffuse to the final color to fix weird issue where things are
      // twice as bright as they should be.
      {
        var diffuseRgba = this.cmbMaterial_.diffuseRgba;
        var diffuseColor =
            new ColorConstant(diffuseRgba.Rf, diffuseRgba.Gf, diffuseRgba.Bf);
        var diffuseAlpha = new ScalarConstant(diffuseRgba.Af);

        this.previousColor_ =
            this.cOps_.Multiply(this.previousColor_, diffuseColor);
        this.previousAlpha_ =
            this.sOps_.Multiply(this.previousAlpha_, diffuseAlpha);
      }

      this.equations_.CreateColorOutput(
          FixedFunctionSource.OUTPUT_COLOR,
          this.previousColor_ ?? this.cOps_.Zero);
      this.equations_.CreateScalarOutput(
          FixedFunctionSource.OUTPUT_ALPHA,
          this.previousAlpha_ ?? this.sOps_.Zero);
    }

    public void AddCombiner_(Combiner cmbCombiner) {
      this.constColorIndex_ = cmbCombiner.constColorIndex;
      this.constColor_ =
          this.cmbMaterial_.constantColors[this.constColorIndex_];

      // Combine values
      var colorSources =
          cmbCombiner.colorSources
                     .Zip(cmbCombiner.colorOperands)
                     .Select(this.GetOppedSourceColor_)
                     .ToArray();
      var newPreviousColor = this.Combine_(
          this.cOps_,
          colorSources,
          cmbCombiner.combinerModeColor,
          cmbCombiner.scaleColor);

      var alphaSources =
          cmbCombiner.alphaSources
                     .Zip(cmbCombiner.alphaOperands)
                     .Select(this.GetOppedSourceAlpha_)
                     .ToArray();
      var newPreviousAlpha = this.Combine_(
          this.sOps_,
          alphaSources,
          cmbCombiner.combinerModeAlpha,
          cmbCombiner.scaleAlpha);

      // Get buffer
      var newPreviousColorBuffer = cmbCombiner.bufferColor switch {
          TexBufferSource.PreviousBuffer => this.previousColorBuffer_,
          TexBufferSource.Previous       => this.previousColor_,
      };
      var newPreviousAlphaBuffer = cmbCombiner.bufferAlpha switch {
          TexBufferSource.PreviousBuffer => this.previousAlphaBuffer_,
          TexBufferSource.Previous       => this.previousAlpha_,
      };

      this.previousColor_ = newPreviousColor;
      this.previousColorBuffer_ = newPreviousColorBuffer;
      this.previousAlpha_ = newPreviousAlpha;
      this.previousAlphaBuffer_ = newPreviousAlphaBuffer;
    }

    private IColorValue? GetOppedSourceColor_(
        (TexCombinerSource combinerSource, TexCombinerColorOp colorOp) input) {
      var (combinerSource, colorOp) = input;

      if (colorOp is TexCombinerColorOp.Color
                     or TexCombinerColorOp.OneMinusColor) {
        var colorValue = this.GetColorValue_(combinerSource);

        if (colorOp is TexCombinerColorOp.OneMinusColor) {
          colorValue = this.cOps_.Subtract(this.cOps_.One, colorValue);
        }

        return colorValue;
      }

      var channelValue = this.GetScalarValue_(
          combinerSource,
          this.GetChannelAndIsOneMinus_(
              (TexCombinerAlphaOp) colorOp));
      return channelValue != null ? new ColorWrapper(channelValue) : null;
    }

    private IScalarValue? GetOppedSourceAlpha_(
        (TexCombinerSource combinerSource, TexCombinerAlphaOp alphaOp) input)
      => this.GetScalarValue_(input.combinerSource,
                              this.GetChannelAndIsOneMinus_(input.alphaOp));

    private IColorValue? GetColorValue_(TexCombinerSource combinerSource)
      => combinerSource switch {
          // TODO: This doesn't appear to be correct based on noclip's implementation
          TexCombinerSource.Texture0 => this.equations_.CreateOrGetColorInput(
              FixedFunctionSource.TEXTURE_COLOR_0),
          TexCombinerSource.Texture1 => this.equations_.CreateOrGetColorInput(
              FixedFunctionSource.TEXTURE_COLOR_1),
          TexCombinerSource.Texture2 => this.equations_.CreateOrGetColorInput(
              FixedFunctionSource.TEXTURE_COLOR_2),
          TexCombinerSource.Texture3 => this.equations_.CreateOrGetColorInput(
              FixedFunctionSource.TEXTURE_COLOR_3),
          TexCombinerSource.Constant =>
              this.registers_.GetOrCreateColorRegister(
                  $"3dsColor{this.constColorIndex_}",
                  this.equations_.CreateColorConstant(
                      this.constColor_.Rf,
                      this.constColor_.Gf,
                      this.constColor_.Bf)),
          TexCombinerSource.PrimaryColor
              => this.equations_.CreateOrGetColorInput(
                  FixedFunctionSource.VERTEX_COLOR_0),
          TexCombinerSource.Previous               => this.previousColor_,
          TexCombinerSource.PreviousBuffer         => this.previousColorBuffer_,
          TexCombinerSource.FragmentPrimaryColor   => this.lightColor_,
          TexCombinerSource.FragmentSecondaryColor => this.lightColor_,
      };

    private IScalarValue? GetScalarValue_(
        TexCombinerSource combinerSource,
        (Channel, bool) channelAndIsOneMinus) {
      IScalarValue? channelValue;

      var (channel, isOneMinus) = channelAndIsOneMinus;
      if (channel != Channel.A) {
        var colorValue = this.GetColorValue_(combinerSource);
        channelValue = channel switch {
            Channel.R => colorValue?.R,
            Channel.G => colorValue?.G,
            Channel.B => colorValue?.B,
        };
      } else {
        channelValue = combinerSource switch {
            // TODO: This doesn't appear to be correct based on noclip's implementation
            TexCombinerSource.Texture0 =>
                this.equations_.CreateOrGetScalarInput(
                    FixedFunctionSource.TEXTURE_ALPHA_0),
            TexCombinerSource.Texture1 =>
                this.equations_.CreateOrGetScalarInput(
                    FixedFunctionSource.TEXTURE_ALPHA_1),
            TexCombinerSource.Texture2 =>
                this.equations_.CreateOrGetScalarInput(
                    FixedFunctionSource.TEXTURE_ALPHA_2),
            TexCombinerSource.Texture3 =>
                this.equations_.CreateOrGetScalarInput(
                    FixedFunctionSource.TEXTURE_ALPHA_3),
            TexCombinerSource.Constant =>
                this.registers_.GetOrCreateScalarRegister(
                    $"3dsAlpha{this.constColorIndex_}",
                    this.equations_.CreateScalarConstant(
                        this.constColor_.Af)),
            TexCombinerSource.PrimaryColor
                => this.equations_.CreateOrGetScalarInput(
                    FixedFunctionSource.VERTEX_ALPHA_0),
            TexCombinerSource.Previous => this.previousAlpha_,
            TexCombinerSource.PreviousBuffer => this.previousAlphaBuffer_,
            TexCombinerSource.FragmentPrimaryColor => this.lightAlpha_,
            TexCombinerSource.FragmentSecondaryColor => this.lightAlpha_,
        };
      }

      if (isOneMinus) {
        channelValue = this.sOps_.Subtract(this.sOps_.One, channelValue);
      }

      return channelValue;
    }

    private enum Channel {
      R,
      G,
      B,
      A
    }

    private (Channel, bool) GetChannelAndIsOneMinus_(
        TexCombinerAlphaOp scalarOp)
      => scalarOp switch {
          TexCombinerAlphaOp.Red           => (Channel.R, false),
          TexCombinerAlphaOp.OneMinusRed   => (Channel.R, true),
          TexCombinerAlphaOp.Green         => (Channel.G, false),
          TexCombinerAlphaOp.OneMinusGreen => (Channel.G, true),
          TexCombinerAlphaOp.Blue          => (Channel.B, false),
          TexCombinerAlphaOp.OneMinusBlue  => (Channel.B, true),
          TexCombinerAlphaOp.Alpha         => (Channel.A, false),
          TexCombinerAlphaOp.OneMinusAlpha => (Channel.A, true),
      };

    private TValue? Combine_<TValue, TConstant, TTerm, TExpression>(
        IFixedFunctionOps<TValue, TConstant, TTerm, TExpression>
            fixedFunctionOps,
        IReadOnlyList<TValue?> sources,
        TexCombineMode combineMode,
        TexCombineScale combineScale)
        where TValue : IValue<TValue, TConstant, TTerm, TExpression>
        where TConstant : IConstant<TValue, TConstant, TTerm, TExpression>,
        TValue
        where TTerm : ITerm<TValue, TConstant, TTerm, TExpression>, TValue
        where TExpression : IExpression<TValue, TConstant, TTerm, TExpression>,
        TValue {
      // TODO: Implement dot-product ones
      var combinedValue = combineMode switch {
          TexCombineMode.Replace => sources[0],
          TexCombineMode.Modulate => fixedFunctionOps.Multiply(
              sources[0],
              sources[1]),
          TexCombineMode.Add => fixedFunctionOps.Add(sources[0], sources[1]),
          TexCombineMode.AddSigned => fixedFunctionOps.Subtract(
              fixedFunctionOps.Add(sources[0], sources[1]),
              fixedFunctionOps.Half),
          TexCombineMode.Subtract => fixedFunctionOps.Subtract(
              sources[0],
              sources[1]),
          TexCombineMode.MultAdd => fixedFunctionOps.Add(
              fixedFunctionOps.Multiply(sources[0], sources[1]),
              sources[2]),
          TexCombineMode.AddMult => fixedFunctionOps.Multiply(
              fixedFunctionOps.Add(sources[0], sources[1]),
              sources[2]),
          TexCombineMode.Interpolate => fixedFunctionOps.Add(
              fixedFunctionOps.Multiply(sources[0],
                                        fixedFunctionOps.Subtract(
                                            fixedFunctionOps.One,
                                            sources[2])),
              fixedFunctionOps.Multiply(sources[1], sources[2])),
          _ => throw new NotImplementedException()
      };

      return combineScale switch {
          TexCombineScale.One => combinedValue,
          TexCombineScale.Two => fixedFunctionOps.MultiplyWithConstant(
              combinedValue,
              2),
          TexCombineScale.Four => fixedFunctionOps.MultiplyWithConstant(
              combinedValue,
              4),
      };
    }
  }
}