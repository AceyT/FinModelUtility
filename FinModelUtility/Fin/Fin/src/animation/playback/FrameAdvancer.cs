﻿using System;

using fin.util.time;

namespace fin.animation.playback {
  public class FrameAdvancer : IAnimationPlaybackManager {
    private readonly IStopwatch impl_ = new FrameStopwatch();

    public double SpeedMultiplier { get; set; } = 1;
    public double Frame { get; set; }

    public int TotalFrames { get; set; }
    public int FrameRate { get; set; }

    private bool isPlaying_ = false;
    public bool IsPlaying {
      get => this.isPlaying_;
      set {
        this.isPlaying_ = value;

        if (value) {
          this.impl_.Start();
        } else {
          this.impl_.Stop();
        }
      }
    }

    public bool ShouldLoop { get; set; }

    
    public void Tick() {
      this.IncrementTowardsNextFrame_();
      this.WrapAround_();
      this.OnUpdate.Invoke();
    }

    public event Action OnUpdate = delegate { };



    public void Reset() {
      this.Frame = 0;
      this.TotalFrames = 0;

      this.isPlaying_ = false;
      this.impl_.Reset();
      this.OnUpdate.Invoke();
    }

    private void IncrementTowardsNextFrame_() {
      if (!this.isPlaying_) {
        return;
      }

      var elapsedSeconds = this.impl_.Elapsed.TotalSeconds;
      var elapsedFrames = elapsedSeconds * this.FrameRate * this.SpeedMultiplier;

      this.Frame += elapsedFrames;

      this.impl_.Restart();
    }

    private void WrapAround_() {
      if (this.Frame < this.TotalFrames) {
        return;
      }
      
      if (!this.ShouldLoop) {
        this.IsPlaying = false;
        this.Frame = 0;
        this.impl_.Reset();
      } else {
        this.Frame %= this.TotalFrames;
      }
    }
  }
}