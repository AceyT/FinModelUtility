﻿namespace gx {
  public enum GxColorChannel : byte {
    GX_COLOR0,
    GX_COLOR1,
    GX_ALPHA0,
    GX_ALPHA1,
    GX_COLOR0A0,
    GX_COLOR1A1,
    GX_COLORZERO,
    GX_BUMP,
    GX_BUMPN,
    GX_COLORNULL,
  }

  public interface ITevOrder {
    byte TexCoordId { get; }
    sbyte TexMap { get; }
    GxColorChannel ColorChannelId { get; }

    GxKonstColorSel KonstColorSel { get; }
    GxKonstAlphaSel KonstAlphaSel { get; }
  }

  public class TevOrderImpl : ITevOrder {
    public byte TexCoordId { get; set; }
    public sbyte TexMap { get; set; }
    public GxColorChannel ColorChannelId { get; set; }
    public GxKonstColorSel KonstColorSel { get; set; }
    public GxKonstAlphaSel KonstAlphaSel { get; set; }
  }
}