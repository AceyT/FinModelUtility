﻿using System.Collections.Generic;

using OpenTK.Graphics;


namespace fin.gl {
  public partial class GlState { }

  public static partial class GlUtil {
    private static Dictionary<IGraphicsContext, GlState>
        stateByContext_ = new();

    private static GlState currentState_;

    public static void SwitchContext(IGraphicsContext context) {
      if (!GlUtil.stateByContext_.TryGetValue(context, out var state)) {
        GlUtil.stateByContext_.Add(context, state = new GlState());
      }

      currentState_ = state;
    }
  }
}