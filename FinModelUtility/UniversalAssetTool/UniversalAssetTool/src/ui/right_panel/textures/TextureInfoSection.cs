﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using fin.image;
using fin.model;
using fin.util.image;

namespace uni.ui.right_panel.textures {
  public partial class TextureInfoSection : UserControl {
    public TextureInfoSection() {
      this.InitializeComponent();

      this.SelectedTexture = null;
    }

    public ITexture? SelectedTexture {
      set => this.propertyGrid_.SelectedObject =
          new PropertyGridTexture(value);
    }

    private class PropertyGridTexture {
      private ITexture? impl_;

      public PropertyGridTexture(ITexture? impl) {
        this.impl_ = impl;
      }

      [Display(Order = 0)]
      [Category("Metadata")]
      public string? Name => this.impl_?.Name;

      [Display(Order = 1)]
      [Category("Metadata")]
      public PixelFormat? PixelFormat => this.impl_?.Image.PixelFormat;

      [Display(Order = 2)]
      [Category("Metadata")]
      public ImageTransparencyType? TransparencyType
        => this.impl_ != null
            ? ImageUtil.GetTransparencyType(this.impl_.Image)
            : null;

      [Display(Order = 3)]
      [Category("Metadata")]
      public int? Width => this.impl_?.Image.Width;
 
      [Display(Order = 4)]
      [Category("Metadata")]
      public int? Height => this.impl_?.Image.Height;

      [Display(Order = 5)]
      [Category("Metadata")]
      public WrapMode? HorizontalWrapMode => this.impl_?.WrapModeU;

      [Display(Order = 6)]
      [Category("Metadata")]
      public WrapMode? VerticalWrapMode => this.impl_?.WrapModeV;

      [Display(Order = 7)]
      [Category("Metadata")]
      public UvType? UvType => this.impl_?.UvType;
    }
  }
}