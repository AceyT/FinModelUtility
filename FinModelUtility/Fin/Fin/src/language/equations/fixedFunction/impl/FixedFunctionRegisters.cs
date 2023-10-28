﻿using System.Collections.Generic;

using fin.color;

namespace fin.language.equations.fixedFunction {
  public class FixedFunctionRegisters : IFixedFunctionRegisters {
    private readonly Dictionary<string, IColorRegister> colorRegistersMap_ =
        new();

    private readonly Dictionary<string, IScalarRegister> scalarRegistersMap_ =
        new();

    private readonly List<IColorRegister> colorRegisters_ = new();
    private readonly List<IScalarRegister> scalarRegisters_ = new();

    public IReadOnlyList<IColorRegister> ColorRegisters
      => this.colorRegisters_;

    public IReadOnlyList<IScalarRegister> ScalarRegisters
      => this.scalarRegisters_;

    public IColorRegister AmbientLightColorRegister { get; }
    public IScalarRegister AmbientLightAmountRegister { get; }

    public FixedFunctionRegisters() {
      this.AmbientLightColorRegister =
          this.GetOrCreateColorRegister("ambientLightColor",
                                        new ColorConstant(1));
      this.AmbientLightAmountRegister =
          this.GetOrCreateScalarRegister("ambientLightAmount",
                                         new ScalarConstant(.1f));
    }

    public IColorRegister GetOrCreateColorRegister(
        string name,
        IColorConstant defaultValue) {
      if (!this.colorRegistersMap_.TryGetValue(name, out var colorRegister)) {
        colorRegister = new ColorRegister(name, defaultValue);

        this.colorRegistersMap_[name] = colorRegister;
        this.colorRegisters_.Add(colorRegister);
      }

      return colorRegister;
    }

    public IScalarRegister GetOrCreateScalarRegister(
        string name,
        IScalarConstant defaultValue) {
      if (!this.scalarRegistersMap_.TryGetValue(name, out var scalarRegister)) {
        scalarRegister = new ScalarRegister(name, defaultValue);

        this.scalarRegistersMap_[name] = scalarRegister;
        this.scalarRegisters_.Add(scalarRegister);
      }

      return scalarRegister;
    }

    private class ColorRegister : BColorValue, IColorRegister {
      public ColorRegister(string name, IColorConstant defaultValue) {
        this.Name = name;
        this.DefaultValue = defaultValue;

        this.Value = defaultValue.IntensityValue != null
            ? FinColor.FromIntensityFloat(
                (float) defaultValue.IntensityValue!)
            : FinColor.FromRgbFloats((float) defaultValue.RValue,
                                     (float) defaultValue.GValue,
                                     (float) defaultValue.BValue);
      }

      public string Name { get; }

      // TODO: Consider switching this to a mutable color value and merging these fields
      public IColorConstant DefaultValue { get; set; }
      public IColor Value { get; set; }

      public IColorValue ColorValue => this.DefaultValue;

      public override IScalarValue? Intensity => this.ColorValue.Intensity;
      public override IScalarValue R => this.ColorValue.R;
      public override IScalarValue G => this.ColorValue.G;
      public override IScalarValue B => this.ColorValue.B;

      public override string ToString() => $"{Name} : {ColorValue}";
    }

    private class ScalarRegister : BScalarValue, IScalarRegister {
      public ScalarRegister(string name, IScalarConstant defaultValue) {
        this.Name = name;
        this.DefaultValue = defaultValue;
        this.Value = (float) defaultValue.Value;
      }

      public string Name { get; }

      // TODO: Consider switching this to a mutable scalar value and merging these fields
      public IScalarConstant DefaultValue { get; set; }
      public float Value { get; set; }

      public IScalarValue ScalarValue => this.DefaultValue;

      public override string ToString() => $"{Name} : {ScalarValue}";
    }
  }
}