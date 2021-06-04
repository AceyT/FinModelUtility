﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace fin.data {
  public class AsyncCollector<T> {
    private readonly List<Task<T>> impl_ = new();

    public void Add(T value) => this.impl_.Add(Task.FromResult(value));
    public void Add(Task<T> value) => this.impl_.Add(value);

    public async Task<T[]> ToArray() => await Task.WhenAll(this.impl_);
  }
}
