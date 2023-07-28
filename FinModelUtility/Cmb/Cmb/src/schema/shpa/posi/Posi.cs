﻿using fin.schema.vector;

using schema.binary;
using schema.binary.attributes;

namespace cmb.schema.shpa.posi {

  [BinarySchema]
  public partial class Posi : IBinaryConvertible {
    [RSequenceUntilEndOfStream]
    public Vector3f[] Values { get; private set; }
  }
}
