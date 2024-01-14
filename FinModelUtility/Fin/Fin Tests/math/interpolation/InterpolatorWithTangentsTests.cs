﻿using NUnit.Framework;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace fin.math.interpolation {
  public class InterpolatorWithTangentsTests {
    [Test]
    public void TestInterpolationStartAndEnd() {
      var fromTime = 1;
      var fromValue = 2;
      var fromTangent = 3;
      var toTime = 4;
      var toValue = 5;
      var toTangent = 6;

      Assert.AreEqual(fromValue,
                      InterpolatorWithTangents.InterpolateFloats(
                          fromTime,
                          fromValue,
                          fromTangent,
                          toTime,
                          toValue,
                          toTangent,
                          fromTime));

      Assert.AreEqual(toValue,
                      InterpolatorWithTangents.InterpolateFloats(
                          fromTime,
                          fromValue,
                          fromTangent,
                          toTime,
                          toValue,
                          toTangent,
                          toTime));
    }
  }
}