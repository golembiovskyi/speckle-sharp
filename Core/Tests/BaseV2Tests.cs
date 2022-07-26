using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using Speckle.Core.Models;
using Tests;
using System.Reflection;

namespace Tests
{
  [TestFixture]
  public class BaseV2Tests
  {
    [Test]
    public void CanGetAndSetProperties()
    {
      var @base = new BaseV2() {
        { "foo", "foo" },
        { "bar", "bar" },
        { "qux", "qux" },
        { "Item", 42 },
        { "💥", "💥" }
      };

      var props = @base.GetProperties().Select(prop => prop.key).ToList();
      
      Assert.AreEqual(props.Count, 5);
      Assert.Contains("foo", props);
      Assert.Contains("bar", props);
      Assert.Contains("qux", props);
      Assert.Contains("Item", props);
      Assert.Contains("💥", props);
    }

    [Test]
    public void SetsPropsInvariant()
    {
      var point = new FOM.Point3D { x = 10, y = 10, z = 10 };

      point["x"] = 20;
      point.y = 20;
      Assert.AreEqual(point.x, 20);
      Assert.AreEqual(point.y, 20);
    }
  }

}

namespace FOM
{
  public class Point3D : BaseV2
  {
    public double x { get; set; }
    public double y { get; set; }
    public double z { get; set; }
  }

  public class Point4D : Point3D
  {
    public double w { get; set; }
  }

  public class Line : BaseV2
  {
    public Point3D start { get; set; }
    public Point3D end { get; set; }
  }
}