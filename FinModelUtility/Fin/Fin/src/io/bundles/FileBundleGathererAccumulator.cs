﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace fin.io.bundles {
  public class AnnotatedFileBundleGathererAccumulator : IAnnotatedFileBundleGathererAccumulator {
    private readonly List<IAnnotatedFileBundleGatherer> gatherers_ = new();

    public IAnnotatedFileBundleGathererAccumulator Add(IAnnotatedFileBundleGatherer gatherer) {
      this.gatherers_.Add(gatherer);
      return this;
    }

    public IAnnotatedFileBundleGathererAccumulator Add(
        Func<IEnumerable<IAnnotatedFileBundle>> handler)
      => this.Add(new AnnotatedFileBundleHandlerGatherer(handler));

    public IEnumerable<IAnnotatedFileBundle> GatherFileBundles()
      => this.gatherers_
             .SelectMany(gatherer => gatherer.GatherFileBundles())
             .ToList();
  }

  public class AnnotatedFileBundleGathererAccumulator<TFileBundle>
      : IAnnotatedFileBundleGathererAccumulator<TFileBundle>
      where TFileBundle : IFileBundle {
    private readonly List<IAnnotatedFileBundleGatherer<TFileBundle>>
        gatherers_ = new();

    public IAnnotatedFileBundleGathererAccumulator<TFileBundle> Add(
        IAnnotatedFileBundleGatherer<TFileBundle> gatherer) {
      this.gatherers_.Add(gatherer);
      return this;
    }

    public IAnnotatedFileBundleGathererAccumulator<TFileBundle> Add(
        Func<IEnumerable<IAnnotatedFileBundle<TFileBundle>>> handler)
      => Add(new AnnotatedFileBundleHandlerGatherer<TFileBundle>(handler));

    public IEnumerable<IAnnotatedFileBundle<TFileBundle>> GatherFileBundles()
      => this.gatherers_
             .SelectMany(gatherer => gatherer.GatherFileBundles())
             .ToList();
  }

  public class AnnotatedFileBundleGathererAccumulatorWithInput<TFileBundle, T>
      : IAnnotatedFileBundleGathererAccumulatorWithInput<TFileBundle, T>
      where TFileBundle : IFileBundle {
    private readonly List<IAnnotatedFileBundleGatherer<TFileBundle>>
        gatherers_ = new();

    private readonly T input_;

    public AnnotatedFileBundleGathererAccumulatorWithInput(T input) {
      this.input_ = input;
    }

    public IAnnotatedFileBundleGathererAccumulatorWithInput<TFileBundle, T> Add(
        IAnnotatedFileBundleGatherer<TFileBundle> gatherer) {
      this.gatherers_.Add(gatherer);
      return this;
    }

    public IAnnotatedFileBundleGathererAccumulatorWithInput<TFileBundle, T> Add(
        Func<IEnumerable<IAnnotatedFileBundle<TFileBundle>>> handler)
      => Add(new AnnotatedFileBundleHandlerGatherer<TFileBundle>(handler));

    public IAnnotatedFileBundleGathererAccumulatorWithInput<TFileBundle, T> Add(
        Func<T, IEnumerable<IAnnotatedFileBundle<TFileBundle>>> handler)
      => Add(new AnnotatedFileBundleHandlerGathererWithInput<TFileBundle, T>(
                 handler,
                 this.input_));

    public IEnumerable<IAnnotatedFileBundle<TFileBundle>> GatherFileBundles()
      => this.gatherers_
             .SelectMany(gatherer => gatherer.GatherFileBundles())
             .ToList();
  }
}