﻿using System.Runtime.CompilerServices;

namespace fin.model {
  public partial class ConsistentVertexAccessor {
    private sealed class NullTangentAccessor
        : BAccessor, IVertexTangentAccessor {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public void Target(IReadOnlyVertex vertex) { }

      public Tangent? LocalTangent => null;
    }
  }
}