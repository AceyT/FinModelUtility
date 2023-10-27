﻿namespace fin.language.equations.fixedFunction {
  public interface IColorNamedValue : INamedValue, IColorFactor {
    IColorValue ColorValue { get; }
  }

  public interface IColorIdentifiedValue<out TIdentifier>
      : IIdentifiedValue<TIdentifier>,
        IColorFactor {
    IColorValue ColorValue { get; }
  }

  public interface IColorInput<out TIdentifier>
      : IColorIdentifiedValue<TIdentifier> {
    IColorConstant DefaultValue { get; }
    IColorConstant? CustomValue { get; set; }
  }

  public interface IColorOutput<out TIdentifier>
      : IColorIdentifiedValue<TIdentifier> { }

  public enum ColorSwizzle {
    R,
    G,
    B,
  }

  public interface IColorNamedValueSwizzle<out TIdentifier> : IScalarFactor {
    IColorIdentifiedValue<TIdentifier> Source { get; }
    ColorSwizzle SwizzleType { get; }
  }

  public interface IColorValueSwizzle : IScalarFactor {
    IColorValue Source { get; }
    ColorSwizzle SwizzleType { get; }
  }


  public interface IColorValue
      : IValue<IColorValue, IColorConstant, IColorTerm, IColorExpression> {
    IScalarValue? Intensity { get; }
    IScalarValue R { get; }
    IScalarValue G { get; }
    IScalarValue B { get; }

    bool Clamp { get; set; }
  }

  public interface IColorTerm
      : IColorValue,
        ITerm<IColorValue, IColorConstant, IColorTerm, IColorExpression> { }

  public interface IColorExpression
      : IColorValue,
        IExpression<IColorValue, IColorConstant, IColorTerm,
            IColorExpression> { }

  public interface IColorFactor : IColorValue { }

  public interface IColorConstant
      : IColorFactor,
        IConstant<IColorValue, IColorConstant, IColorTerm, IColorExpression> {
    double? IntensityValue { get; }
    double RValue { get; }
    double GValue { get; }
    double BValue { get; }
  }
}