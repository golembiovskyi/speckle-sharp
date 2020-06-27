using Speckle.Serialisation;
using System.Collections.Generic;
using Speckle.Kits;
using Speckle.Transports;
using System.Diagnostics;
using NUnit.Framework;
using Speckle.Models;
using Newtonsoft.Json;
using System.Dynamic;
using Speckle.Core;

namespace Tests
{
  [TestFixture]
  public class Serialization
  {

    [Test]
    public void SimpleSerialization()
    {
      var table = new DiningTable();
      ((dynamic)table)["@strangeVariable_NAme3"] = new TableLegFixture();

      var result = Operations.Serialize(table);
      var test = Operations.Deserialize(result);

      Assert.AreEqual(test.GetId(), table.GetId());

      var polyline = new Polyline();
      for (int i = 0; i < 100; i++)
        polyline.Points.Add(new Point() { X = i * 2, Y = i % 2 });

      var strPoly = Operations.Serialize(polyline);
      var dePoly = Operations.Deserialize(strPoly);

      Assert.AreEqual(polyline.GetId(), dePoly.GetId());
    }

    [Test]
    public void ListSerialisation()
    {
      var objs = new List<Base>();
      for (int i = 0; i < 10; i++)
        objs.Add(new Point(i, i, i));

      var result = Operations.Serialize(objs);
      var test = Operations.DeserializeArray(result);
      Assert.AreEqual(10, test.Count);
    }

    //[Test]
    //public void TreeTrackingTest()
    //{
    //  var d5 = new Base();
    //  ((dynamic)d5).name = "depth five"; // end v

    //  var d4 = new Base();
    //  ((dynamic)d4).name = "depth four";
    //  ((dynamic)d4)["@detach"] = d5;

    //  var d3 = new Base();
    //  ((dynamic)d3).name = "depth three";
    //  ((dynamic)d3)["@detach"] = d4;

    //  var d2 = new Base();
    //  ((dynamic)d2).name = "depth two";
    //  ((dynamic)d2)["@detach"] = d3;
    //  ((dynamic)d2)["@joker"] = new object[] { d5 };

    //  var d1 = new Base();
    //  ((dynamic)d1).name = "depth one"; 
    //  ((dynamic)d1)["@detach"] = d2;
    //  ((dynamic)d1)["@joker"] = d5; // consequently, d5 depth in d1 should be 1

    //  var transport = new MemoryTransport();
    //  var serializer = new Serializer();

    //  var result = serializer.SerializeAndSave(d1, transport);

    //  var test = serializer.DeserializeAndGet(result, transport);

    //  Assert.AreEqual(test.hash, d1.hash);

    //  var d1_ = JsonConvert.DeserializeObject<dynamic>(result);
    //  var d2_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d2.hash]);
    //  var d3_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d3.hash]);
    //  var d4_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d4.hash]);
    //  var d5_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d5.hash]);


    //  var depthOf_d5_in_d1 = int.Parse( (string) d1_.__closure[d5.hash] );
    //  Assert.AreEqual(1, depthOf_d5_in_d1);

    //  var depthOf_d4_in_d1 = int.Parse((string)d1_.__closure[d4.hash]);
    //  Assert.AreEqual(3, depthOf_d4_in_d1);

    //  var depthOf_d5_in_d3 = int.Parse((string)d3_.__closure[d5.hash]);
    //  Assert.AreEqual(2, depthOf_d5_in_d3);

    //  var depthOf_d4_in_d3 = int.Parse((string)d3_.__closure[d4.hash]);
    //  Assert.AreEqual(1, depthOf_d4_in_d3);

    //  var depthOf_d5_in_d2= int.Parse((string)d2_.__closure[d5.hash]);
    //  Assert.AreEqual(1, depthOf_d5_in_d2);

    //  var sampleData = "[";
    //  foreach(var kvp in transport.Objects)
    //  {
    //    sampleData += kvp.Value + ", ";
    //  }
    //  sampleData += "]";
    //  var copy = sampleData;
    //}

    //private class ClosureTreeHelper
    //{
    //  public Dictionary<string, Dictionary<string, int>> __closure { get; set; }
    //}

    //[Test]
    //public void DynamicDispatchment()
    //{
    //  var pt = new Point(1, 2, 3);
    //  ((dynamic)pt).HelloWorld = "whatever";
    //  ((dynamic)pt)["@detach_me"] = new Point(3, 4, 5);
    //  ((dynamic)pt)["@detach_me_too"] = new Point(3, 4, 5); // same point, same hash, should not create a new object in the transport.

    //  var transport = new MemoryTransport();
    //  var serializer = new Serializer();

    //  var result = serializer.SerializeAndSave(pt, transport);

    //  Assert.AreEqual(2, transport.Objects.Count);

    //  var deserialized = serializer.DeserializeAndGet(result, transport);

    //  Assert.AreEqual(pt.hash, deserialized.hash);
    //  Assert.AreEqual(((dynamic)pt).HelloWorld, "whatever");
    //}

    [Test]
    public void SerialisationAbstractObjects()
    {
      var nk = new NonKitClass() { TestProp = "Hello", Numbers = new List<int>() { 1, 2, 3, 4, 5 } };
      var abs = new Abstract(nk);

      var transport = new MemoryTransport();

      var abs_serialized = Operations.Serialize(abs);
      var abs_deserialized = Operations.Deserialize(abs_serialized);
      var abs_se_deserializes = Operations.Serialize(abs_deserialized);

      Assert.AreEqual(abs.GetId(), abs_deserialized.GetId());
      Assert.AreEqual(abs.@base.GetType(), ((Abstract)abs_deserialized).@base.GetType());
    }

    [Test]
    public void IgnoreCircularReferences()
    {
      var pt = new Point(1, 2, 3);
      ((dynamic)pt).circle = pt;

      var test = Operations.Serialize(pt);

      var result = Operations.Deserialize(test);
      var circle = ((dynamic)result).circle;

      Assert.Null(circle);
    }

  }
}
