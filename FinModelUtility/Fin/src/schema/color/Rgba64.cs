﻿using schema;


namespace fin.schema.color {
  [BinarySchema]
  public partial class Rgba64 : IBiSerializable {
    public ushort R { get; set; }
    public ushort G { get; set; }
    public ushort B { get; set; }
    public ushort A { get; set; }
  }
}
