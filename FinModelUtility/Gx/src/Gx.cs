﻿namespace gx {
  /// <summary>
  ///   Shamelessly copied from https://github.com/magcius/noclip.website/blob/1fa39ab5d9d014095c77a8509bf6ba87d3296200/src/gx/gx_enum.ts
  /// </summary>

  public enum GxOpcode : byte {
    NOP = 0x0,

    DRAW_QUADS = 0x80,
    // Early code for GX_DRAW_QUADS? Seen in Luigi's Mansion.
    DRAW_QUADS_2 = 0x88,
    DRAW_TRIANGLES = 0x90,
    DRAW_TRIANGLE_STRIP = 0x98,
    DRAW_TRIANGLE_FAN = 0xA0,
    DRAW_LINES = 0xA8,
    DRAW_LINE_STRIP = 0xB0,
    DRAW_POINTS = 0xB8,

    LOAD_INDX_A = 0x20,
    LOAD_INDX_B = 0x28,
    LOAD_INDX_C = 0x30,
    LOAD_INDX_D = 0x38,

    LOAD_BP_REG = 0x61,
    LOAD_CP_REG = 0x08,
    LOAD_XF_REG = 0x10,
  }

  public enum GxAttributeType {
    NOT_PRESENT = 0,
    DIRECT = 1,
    INDEX_8 = 2,
    INDEX_16 = 3,
  }

  public enum GxAttribute {
    PNMTXIDX = 0,
    TEX0MTXIDX = 1,
    TEX1MTXIDX = 2,
    TEX2MTXIDX = 3,
    TEX3MTXIDX = 4,
    TEX4MTXIDX = 5,
    TEX5MTXIDX = 6,
    TEX6MTXIDX = 7,
    TEX7MTXIDX = 8,
    POS = 9,
    NRM = 10,
    CLR0 = 11,
    CLR1 = 12,
    TEX0 = 13,
    TEX1 = 14,
    TEX2 = 15,
    TEX3 = 16,
    TEX4 = 17,
    TEX5 = 18,
    TEX6 = 19,
    TEX7 = 20,
    MAX = TEX7,
    // NOTE: NBT is a fake API entry-point for GX! It's listed here in case some tools
    // use it as serialization, but you should always switch to NRM in your high-level code.
    _NBT = 25,
    NULL = 0xFF,
  }

  public enum GxComponentCount {
    // Position
    POS_XY = 0,
    POS_XYZ = 1,
    // Normal
    NRM_XYZ = 0,
    NRM_NBT = 1,
    NRM_NBT3 = 2,
    // Color
    CLR_RGB = 0,
    CLR_RGBA = 1,
    // TexCoord
    TEX_S = 0,
    TEX_ST = 1,
  }

  public enum GxComponentType {
    U8 = 0,
    S8 = 1,
    U16 = 2,
    S16 = 3,
    F32 = 4,

    RGB565 = 0,
    RGB8 = 1,
    RGBX8 = 2,
    RGBA4 = 3,
    RGBA6 = 4,
    RGBA8 = 5,
  }

  public enum GxCc : byte {
    GX_CC_CPREV,
    GX_CC_APREV,
    GX_CC_C0,
    GX_CC_A0,
    GX_CC_C1,
    GX_CC_A1,
    GX_CC_C2,
    GX_CC_A2,
    GX_CC_TEXC,
    GX_CC_TEXA,
    GX_CC_RASC,
    GX_CC_RASA,
    GX_CC_ONE,
    GX_CC_HALF,
    GX_CC_KONST,
    GX_CC_ZERO,
  }

  public enum GxCa : byte {
    GX_CA_APREV,
    GX_CA_A0,
    GX_CA_A1,
    GX_CA_A2,
    GX_CA_TEXA,
    GX_CA_RASA,
    GX_CA_KONST,
    GX_CA_ZERO,
  }

  public enum TevOp : byte {
    GX_TEV_ADD = 0,
    GX_TEV_SUB = 1,
    GX_TEV_COMP_R8_GT = 8,
    GX_TEV_COMP_R8_EQ = 9,
    GX_TEV_COMP_GR16_GT = 10,
    GX_TEV_COMP_GR16_EQ = 11,
    GX_TEV_COMP_BGR24_GT = 12,
    GX_TEV_COMP_BGR24_EQ = 13,
    GX_TEV_COMP_RGB8_GT = 14,
    GX_TEV_COMP_RGB8_EQ = 15
  }

  public enum TevBias : byte {
    GX_TB_ZERO,
    GX_TB_ADDHALF,
    GX_TB_SUBHALF
  }

  public enum TevScale : byte {
    GX_CS_SCALE_1,
    GX_CS_SCALE_2,
    GX_CS_SCALE_4,
    GX_CS_DIVIDE_2
  }

  public enum ColorRegister : byte {
    GX_TEVPREV,
    GX_TEVREG0,
    GX_TEVREG1,
    GX_TEVREG2,
  }

  public enum GxKonstColorSel {
    KCSel_1 = 0x00,     // Constant 1.0
    KCSel_7_8 = 0x01,   // Constant 7/8
    KCSel_3_4 = 0x02,   // Constant 3/4
    KCSel_5_8 = 0x03,   // Constant 5/8
    KCSel_1_2 = 0x04,   // Constant 1/2
    KCSel_3_8 = 0x05,   // Constant 3/8
    KCSel_1_4 = 0x06,   // Constant 1/4
    KCSel_1_8 = 0x07,   // Constant 1/8
    KCSel_K0 = 0x0C,    // K0[RGB] Register
    KCSel_K1 = 0x0D,    // K1[RGB] Register
    KCSel_K2 = 0x0E,    // K2[RGB] Register
    KCSel_K3 = 0x0F,    // K3[RGB] Register
    KCSel_K0_R = 0x10,  // K0[RRR] Register
    KCSel_K1_R = 0x11,  // K1[RRR] Register
    KCSel_K2_R = 0x12,  // K2[RRR] Register
    KCSel_K3_R = 0x13,  // K3[RRR] Register
    KCSel_K0_G = 0x14,  // K0[GGG] Register
    KCSel_K1_G = 0x15,  // K1[GGG] Register
    KCSel_K2_G = 0x16,  // K2[GGG] Register
    KCSel_K3_G = 0x17,  // K3[GGG] Register
    KCSel_K0_B = 0x18,  // K0[BBB] Register
    KCSel_K1_B = 0x19,  // K1[BBB] Register
    KCSel_K2_B = 0x1A,  // K2[BBB] Register
    KCSel_K3_B = 0x1B,  // K3[BBB] Register
    KCSel_K0_A = 0x1C,  // K0[AAA] Register
    KCSel_K1_A = 0x1D,  // K1[AAA] Register
    KCSel_K2_A = 0x1E,  // K2[AAA] Register
    KCSel_K3_A = 0x1F   // K3[AAA] Register
  }

  public enum GxKonstAlphaSel {
    KASel_1 = 0x00,     // Constant 1.0
    KASel_7_8 = 0x01,   // Constant 7/8
    KASel_3_4 = 0x02,   // Constant 3/4
    KASel_5_8 = 0x03,   // Constant 5/8
    KASel_1_2 = 0x04,   // Constant 1/2
    KASel_3_8 = 0x05,   // Constant 3/8
    KASel_1_4 = 0x06,   // Constant 1/4
    KASel_1_8 = 0x07,   // Constant 1/8
    KASel_K0_R = 0x10,  // K0[R] Register
    KASel_K1_R = 0x11,  // K1[R] Register
    KASel_K2_R = 0x12,  // K2[R] Register
    KASel_K3_R = 0x13,  // K3[R] Register
    KASel_K0_G = 0x14,  // K0[G] Register
    KASel_K1_G = 0x15,  // K1[G] Register
    KASel_K2_G = 0x16,  // K2[G] Register
    KASel_K3_G = 0x17,  // K3[G] Register
    KASel_K0_B = 0x18,  // K0[B] Register
    KASel_K1_B = 0x19,  // K1[B] Register
    KASel_K2_B = 0x1A,  // K2[B] Register
    KASel_K3_B = 0x1B,  // K3[B] Register
    KASel_K0_A = 0x1C,  // K0[A] Register
    KASel_K1_A = 0x1D,  // K1[A] Register
    KASel_K2_A = 0x1E,  // K2[A] Register
    KASel_K3_A = 0x1F   // K3[A] Register
  }

  public enum GX_WRAP_TAG : byte {
    GX_CLAMP,
    GX_REPEAT,
    GX_MIRROR,
    GX_MAXTEXWRAPMODE,
  }

  public enum GX_TEXTURE_FILTER : byte {
    GX_NEAR,
    GX_LINEAR,
    GX_NEAR_MIP_NEAR,
    GX_LIN_MIP_NEAR,
    GX_NEAR_MIP_LIN,
    GX_LIN_MIP_LIN,
    GX_NEAR2,
    GX_NEAR3,
  }
}