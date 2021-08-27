﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using Objects.Geometry;
using Objects.Structural;
using Objects.Structural.Geometry;
using Restraint = Objects.Structural.Geometry.Restraint;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using MemberType = Objects.Structural.Geometry.MemberType;
using Xunit.Sdk;
using Speckle.Core.Kits;

namespace ConverterGSATests
{
  public class SchemaTest : SpeckleConversionFixture
  {
    public SchemaTest() : base() { }

    //Reminder: conversions could create 1:1, 1:n, n:1, n:n structural per native objects

    #region Geometry
    [Fact]
    public void GsaAxis()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaAxis = GsaAxisExample("axis 1");
      gsaRecords.Add(gsaAxis);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Axis);

      var speckleAxis = (Axis)structuralObjects.FirstOrDefault(so => so is Axis);

      Assert.Equal("axis 1", speckleAxis.applicationId);
      Assert.Equal(AxisType.Cartesian, speckleAxis.axisType);
      Assert.Equal(gsaAxis.OriginX, speckleAxis.definition.origin.x);
      Assert.Equal(gsaAxis.OriginY, speckleAxis.definition.origin.y);
      Assert.Equal(gsaAxis.OriginZ, speckleAxis.definition.origin.z);
      Assert.Equal(gsaAxis.XDirX.Value, speckleAxis.definition.xdir.x, 6);
      Assert.Equal(gsaAxis.XDirY.Value, speckleAxis.definition.xdir.y, 6);
      Assert.Equal(gsaAxis.XDirZ.Value, speckleAxis.definition.xdir.z, 6);
      Assert.Equal(gsaAxis.XYDirX.Value, speckleAxis.definition.ydir.x, 6);
      Assert.Equal(gsaAxis.XYDirY.Value, speckleAxis.definition.ydir.y, 6);
      Assert.Equal(gsaAxis.XYDirZ.Value, speckleAxis.definition.ydir.z, 6);
    }
    
    [Fact]
    public void GsaNode()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      var gsaNodes = GsaNodeExamples(1, "node 1");
      gsaRecords.AddRange(gsaNodes);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Node);

      var speckleNode = (Node)structuralObjects.FirstOrDefault(so => so is Node);

      //Base properties
      Assert.Equal("node 1", speckleNode.applicationId);
      Assert.Equal(gsaNodes[0].Name, speckleNode.name);
      Assert.Equal(gsaNodes[0].X, speckleNode.basePoint.x);
      Assert.Equal(gsaNodes[0].Y, speckleNode.basePoint.y);
      Assert.Equal(gsaNodes[0].Z, speckleNode.basePoint.z);
      Assert.Equal(new Restraint(RestraintType.Pinned).code, speckleNode.restraint.code);

      //Axis - global
      Assert.Equal(0, speckleNode.constraintAxis.origin.x);
      Assert.Equal(0, speckleNode.constraintAxis.origin.y);
      Assert.Equal(0, speckleNode.constraintAxis.origin.z);
      Assert.Equal(0, speckleNode.constraintAxis.normal.x);
      Assert.Equal(0, speckleNode.constraintAxis.normal.y);
      Assert.Equal(1, speckleNode.constraintAxis.normal.z);
      Assert.Equal(1, speckleNode.constraintAxis.xdir.x);
      Assert.Equal(0, speckleNode.constraintAxis.xdir.y);
      Assert.Equal(0, speckleNode.constraintAxis.xdir.z);
      Assert.Equal(0, speckleNode.constraintAxis.ydir.x);
      Assert.Equal(1, speckleNode.constraintAxis.ydir.y);
      Assert.Equal(0, speckleNode.constraintAxis.ydir.z);

      //Dynamic properties
      Assert.Equal(gsaNodes[0].Colour.ToString(), speckleNode["colour"]);
      Assert.Equal(gsaNodes[0].MeshSize.Value, speckleNode["localElementSize"]);
      var specklePropertySpring = speckleNode["propertySpring"] as PropertySpring;
      Assert.Equal("property spring 1", specklePropertySpring.applicationId); //assume conversion code for GsaPropSpr is tested elsewhere
      var specklePropertyMass = speckleNode["propertyMass"] as PropertyMass;
      Assert.Equal("property mass 1", specklePropertyMass.applicationId); //assume conversion code for GsaPropMass is tested elsewhere
    }

    [Fact]
    public void GsaElement2d()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      var gsaNodes = GsaNodeExamples(5, "node 1", "node 2", "node 3", "node 4" , "node 5");
      gsaRecords.AddRange(gsaNodes);
      var gsaProp2d = GsaProp2dExample("property 2D 1");
      gsaRecords.Add(gsaProp2d);

      // Gen #3
      var gsaEls = GsaElement2dExamples(2, "element 1", "element 2");
      gsaRecords.AddRange(gsaEls);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(o => o.applicationId, o => (object) o));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Element2D);

      var speckleElement2D = structuralObjects.FindAll(so => so is Element2D).Select(so => (Element2D)so).ToList();

      //===========
      // Element 1
      //===========
      Assert.Equal("element 1", speckleElement2D[0].applicationId);
      Assert.Equal(gsaEls[0].Name, speckleElement2D[0].name);
      //baseMesh
      Assert.Equal("property 2D 1", speckleElement2D[0].property.applicationId);
      Assert.Equal(ElementType2D.Quad4, speckleElement2D[0].type);
      Assert.Equal(gsaEls[0].OffsetZ.Value, speckleElement2D[0].offset);
      Assert.Equal(gsaEls[0].Angle.Value, speckleElement2D[0].orientationAngle);
      //parent
      Assert.Equal("node 1", speckleElement2D[0].topology[0].applicationId);
      Assert.Equal(gsaNodes[0].X, speckleElement2D[0].topology[0].basePoint.x);
      Assert.Equal(gsaNodes[0].Y, speckleElement2D[0].topology[0].basePoint.y);
      Assert.Equal(gsaNodes[0].Z, speckleElement2D[0].topology[0].basePoint.z);
      Assert.Equal("node 2", speckleElement2D[0].topology[1].applicationId);
      Assert.Equal(gsaNodes[1].X, speckleElement2D[0].topology[1].basePoint.x);
      Assert.Equal(gsaNodes[1].Y, speckleElement2D[0].topology[1].basePoint.y);
      Assert.Equal(gsaNodes[1].Z, speckleElement2D[0].topology[1].basePoint.z);
      Assert.Equal("node 3", speckleElement2D[0].topology[2].applicationId);
      Assert.Equal(gsaNodes[2].X, speckleElement2D[0].topology[2].basePoint.x);
      Assert.Equal(gsaNodes[2].Y, speckleElement2D[0].topology[2].basePoint.y);
      Assert.Equal(gsaNodes[2].Z, speckleElement2D[0].topology[2].basePoint.z);
      Assert.Equal("node 4", speckleElement2D[0].topology[3].applicationId);
      Assert.Equal(gsaNodes[3].X, speckleElement2D[0].topology[3].basePoint.x);
      Assert.Equal(gsaNodes[3].Y, speckleElement2D[0].topology[3].basePoint.y);
      Assert.Equal(gsaNodes[3].Z, speckleElement2D[0].topology[3].basePoint.z);
      //displayMesh

      //===========
      // Element 2
      //===========
      Assert.Equal("element 2", speckleElement2D[1].applicationId);
      Assert.Equal(gsaEls[0].Name, speckleElement2D[1].name);
      //baseMesh
      Assert.Equal("property 2D 1", speckleElement2D[1].property.applicationId);
      Assert.Equal(ElementType2D.Triangle3, speckleElement2D[1].type);
      Assert.Equal(gsaEls[0].OffsetZ.Value, speckleElement2D[1].offset);
      Assert.Equal(gsaEls[0].Angle.Value, speckleElement2D[1].orientationAngle);
      //parent
      Assert.Equal("node 1", speckleElement2D[1].topology[0].applicationId);
      Assert.Equal(gsaNodes[0].X, speckleElement2D[1].topology[0].basePoint.x);
      Assert.Equal(gsaNodes[0].Y, speckleElement2D[1].topology[0].basePoint.y);
      Assert.Equal(gsaNodes[0].Z, speckleElement2D[1].topology[0].basePoint.z);
      Assert.Equal("node 2", speckleElement2D[1].topology[1].applicationId);
      Assert.Equal(gsaNodes[1].X, speckleElement2D[1].topology[1].basePoint.x);
      Assert.Equal(gsaNodes[1].Y, speckleElement2D[1].topology[1].basePoint.y);
      Assert.Equal(gsaNodes[1].Z, speckleElement2D[1].topology[1].basePoint.z);
      Assert.Equal("node 5", speckleElement2D[1].topology[2].applicationId);
      Assert.Equal(gsaNodes[4].X, speckleElement2D[1].topology[2].basePoint.x);
      Assert.Equal(gsaNodes[4].Y, speckleElement2D[1].topology[2].basePoint.y);
      Assert.Equal(gsaNodes[4].Z, speckleElement2D[1].topology[2].basePoint.z);
      //displayMesh
    }

    [Fact]
    public void GsaElement1d()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));
      gsaRecords.Add(GsaPropMassExample("property mass 1"));
      gsaRecords.Add(GsaPropSprExample("property spring 1"));

      //Gen #2
      var gsaNodes = GsaNodeExamples(3, "node 1", "node 2", "node 3");
      gsaRecords.AddRange(gsaNodes);
      var gsaSection = GsaCatalogueSectionExample("section 1");
      gsaRecords.Add(gsaSection);

      // Gen #3
      var gsaEls = GsaElement1dExamples(2, "element 1", "element 2");
      gsaRecords.AddRange(gsaEls);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Element1D);

      var speckleElement1d = structuralObjects.FindAll(so => so is Element1D).Select(so => (Element1D)so).ToList();

      //Checks - Element 1
      Assert.Equal("element 1", speckleElement1d[0].applicationId);
      Assert.Equal("node 1", speckleElement1d[0].end1Node.applicationId); //assume conversion to Node is tested elsewhere
      Assert.Equal("node 2", speckleElement1d[0].end2Node.applicationId); //assume conversion to Node is tested elsewhere
      Assert.Equal("node 1", speckleElement1d[0].topology[0].applicationId);
      Assert.Equal("node 2", speckleElement1d[0].topology[1].applicationId);
      Assert.Equal(0, speckleElement1d[0].end1Offset.x);
      Assert.Equal(0, speckleElement1d[0].end1Offset.y);
      Assert.Equal(0, speckleElement1d[0].end1Offset.z);
      Assert.Equal(0, speckleElement1d[0].end2Offset.x);
      Assert.Equal(0, speckleElement1d[0].end2Offset.y);
      Assert.Equal(0, speckleElement1d[0].end2Offset.z);
      Assert.Equal("FFFFFF", speckleElement1d[0].end1Releases.code);
      Assert.Equal("FFFFFF", speckleElement1d[0].end2Releases.code);
      Assert.Equal(0, speckleElement1d[0].localAxis.origin.x, 6);
      Assert.Equal(0, speckleElement1d[0].localAxis.origin.y, 6);
      Assert.Equal(0, speckleElement1d[0].localAxis.origin.z, 6);
      Assert.Equal(1, speckleElement1d[0].localAxis.xdir.x, 6);
      Assert.Equal(0, speckleElement1d[0].localAxis.xdir.y, 6);
      Assert.Equal(0, speckleElement1d[0].localAxis.xdir.z, 6);
      Assert.Equal(0, speckleElement1d[0].localAxis.ydir.x, 6);
      Assert.Equal(1, speckleElement1d[0].localAxis.ydir.y, 6);
      Assert.Equal(0, speckleElement1d[0].localAxis.ydir.z, 6);
      Assert.Equal(0, speckleElement1d[0].localAxis.normal.x, 6);
      Assert.Equal(0, speckleElement1d[0].localAxis.normal.y, 6);
      Assert.Equal(1, speckleElement1d[0].localAxis.normal.z, 6);
      Assert.Equal(0, speckleElement1d[0].orientationAngle);
      Assert.Null(speckleElement1d[0].orientationNode);
      Assert.Equal("section 1", speckleElement1d[0].property.applicationId); //assume conversion to Property1d is tested elsewhere

      //Checks - Element 2
      Assert.Equal("element 2", speckleElement1d[1].applicationId);
      Assert.Equal("node 2", speckleElement1d[1].end1Node.applicationId); //assume conversion to Node is tested elsewhere
      Assert.Equal("node 3", speckleElement1d[1].end2Node.applicationId); //assume conversion to Node is tested elsewhere
      Assert.Equal("node 2", speckleElement1d[1].topology[0].applicationId);
      Assert.Equal("node 3", speckleElement1d[1].topology[1].applicationId);
      Assert.Equal(0, speckleElement1d[1].end1Offset.x);
      Assert.Equal(0, speckleElement1d[1].end1Offset.y);
      Assert.Equal(0, speckleElement1d[1].end1Offset.z);
      Assert.Equal(0, speckleElement1d[1].end2Offset.x);
      Assert.Equal(0, speckleElement1d[1].end2Offset.y);
      Assert.Equal(0, speckleElement1d[1].end2Offset.z);
      Assert.Equal("FFFFFF", speckleElement1d[1].end1Releases.code);
      Assert.Equal("FFFFFF", speckleElement1d[1].end2Releases.code);
      Assert.Equal(1, speckleElement1d[1].localAxis.origin.x, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.origin.y, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.origin.z, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.xdir.x, 6);
      Assert.Equal(1, speckleElement1d[1].localAxis.xdir.y, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.xdir.z, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.ydir.x, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.ydir.y, 6);
      Assert.Equal(1, speckleElement1d[1].localAxis.ydir.z, 6);
      Assert.Equal(1, speckleElement1d[1].localAxis.normal.x, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.normal.y, 6);
      Assert.Equal(0, speckleElement1d[1].localAxis.normal.z, 6);
      Assert.Equal(90, speckleElement1d[1].orientationAngle);
      Assert.Null(speckleElement1d[1].orientationNode);
      Assert.Equal("section 1", speckleElement1d[1].property.applicationId); //assume conversion to Property1d is tested elsewhere
    }
    #endregion

    #region Loading
    #endregion

    #region Materials
    [Fact]
    public void GsaMatConcrete()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaMatConcrete = GsaMatConcreteExample("concrete 1");
      gsaRecords.Add(gsaMatConcrete);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Concrete);

      var speckleConcrete = (Concrete)structuralObjects.FirstOrDefault(so => so is Concrete);

      Assert.Equal("concrete 1", speckleConcrete.applicationId);
      Assert.Equal("", speckleConcrete.grade);
      Assert.Equal(MaterialType.Concrete, speckleConcrete.type);
      Assert.Equal("", speckleConcrete.designCode);
      Assert.Equal("", speckleConcrete.codeYear);
      Assert.Equal(gsaMatConcrete.Fc.Value, speckleConcrete.compressiveStrength);
      Assert.Equal(gsaMatConcrete.Mat.Rho.Value, speckleConcrete.density);
      Assert.Equal(gsaMatConcrete.Mat.E.Value, speckleConcrete.youngsModulus);
      Assert.Equal(gsaMatConcrete.Mat.G.Value, speckleConcrete.shearModulus);
      Assert.Equal(gsaMatConcrete.Mat.Nu.Value, speckleConcrete.poissonsRatio);
      Assert.Equal(gsaMatConcrete.Mat.Alpha.Value, speckleConcrete.thermalExpansivity);
      Assert.Equal(gsaMatConcrete.EpsU.Value, speckleConcrete.maxStrain);
      Assert.Equal(gsaMatConcrete.Agg.Value, speckleConcrete.maxAggregateSize);
      Assert.Equal(gsaMatConcrete.Fcdt.Value, speckleConcrete.tensileStrength);
      Assert.Equal(0, speckleConcrete.flexuralStrength);
    }

    [Fact]
    public void GsaMatSteel()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaMatSteel = GsaMatSteelExample("steel 1");
      gsaRecords.Add(gsaMatSteel);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Steel);

      var speckleSteel = (Steel)structuralObjects.FirstOrDefault(so => so is Steel);

      Assert.Equal("steel 1", speckleSteel.applicationId);
      Assert.Equal("", speckleSteel.grade);
      Assert.Equal(MaterialType.Steel, speckleSteel.type);
      Assert.Equal("", speckleSteel.designCode);
      Assert.Equal("", speckleSteel.codeYear);
      Assert.Equal(gsaMatSteel.Fy.Value, speckleSteel.yieldStrength);
      Assert.Equal(gsaMatSteel.Fu.Value, speckleSteel.ultimateStrength);
      Assert.Equal(gsaMatSteel.EpsP.Value, speckleSteel.maxStrain);
      Assert.Equal(gsaMatSteel.Mat.E.Value, speckleSteel.youngsModulus);
      Assert.Equal(gsaMatSteel.Mat.Nu.Value, speckleSteel.poissonsRatio);
      Assert.Equal(gsaMatSteel.Mat.G.Value, speckleSteel.shearModulus);
      Assert.Equal(gsaMatSteel.Mat.Rho.Value, speckleSteel.density);
      Assert.Equal(gsaMatSteel.Mat.Alpha.Value, speckleSteel.thermalExpansivity);
    }

    //Timber not yet supported
    #endregion

    #region Property
    [Fact]
    public void GsaProperty1D()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));

      //Gen #2
      var gsaSection = GsaCatalogueSectionExample("section 1");
      gsaRecords.Add(gsaSection);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Property1D);

      var speckleProperty1D = (Property1D)structuralObjects.FirstOrDefault(so => so is Property1D);

      //Checks
      Assert.Equal("section 1", speckleProperty1D.applicationId);
      Assert.Equal(gsaSection.Colour.ToString(), speckleProperty1D.colour);
      Assert.Equal(MemberType.Generic1D, speckleProperty1D.memberType);
      Assert.Equal("steel material 1", speckleProperty1D.material.applicationId); //assume tests are done elsewhere
      Assert.Equal("", speckleProperty1D.grade);
      Assert.Equal(((ProfileDetailsCatalogue)((SectionComp)gsaSection.Components[0]).ProfileDetails).Profile, speckleProperty1D.profile.shapeDescription);
      Assert.Equal(BaseReferencePoint.Centroid, speckleProperty1D.referencePoint);
      Assert.Equal(0, speckleProperty1D.offsetY);
      Assert.Equal(0, speckleProperty1D.offsetZ);
      Assert.Equal(0, speckleProperty1D.area);
      Assert.Equal(0, speckleProperty1D.Iyy);
      Assert.Equal(0, speckleProperty1D.Izz);
      Assert.Equal(0, speckleProperty1D.J);
      Assert.Equal(0, speckleProperty1D.Ky);
      Assert.Equal(0, speckleProperty1D.Kz);

      //TO DO: update once modifiers have been included in the section definition
    }

    [Fact]
    public void GsaProperty2D()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      gsaRecords.Add(GsaMatSteelExample("steel material 1"));

      //Gen #2
      var gsaProp2d = GsaProp2dExample("property 2D 1");
      gsaRecords.Add(gsaProp2d);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is Property2D);

      var speckleProperty2D = (Property2D)structuralObjects.FirstOrDefault(so => so is Property2D);

      //Check base properties
      Assert.Equal("property 2D 1", speckleProperty2D.applicationId);
      Assert.Equal(gsaProp2d.Name, speckleProperty2D.name);
      Assert.Equal(gsaProp2d.Colour.ToString(), speckleProperty2D.colour);
      Assert.Equal(gsaProp2d.Thickness.Value, speckleProperty2D.thickness);
      Assert.Equal("steel material 1", speckleProperty2D.material.applicationId); //assume conversion of material is covered by another test
      Assert.Equal("", speckleProperty2D.grade);
      Assert.Null(speckleProperty2D.orientationAxis.applicationId); //no application ID for global coordinate system
      Assert.Equal(PropertyType2D.Shell, speckleProperty2D.type);
      Assert.Equal(ReferenceSurface.Middle, speckleProperty2D.refSurface);
      Assert.Equal(gsaProp2d.RefZ, speckleProperty2D.zOffset);

      //Check modifiers (currently no way to distinguish between value and percentage in speckle object)
      Assert.Equal(gsaProp2d.InPlaneStiffnessPercentage.Value, speckleProperty2D.modifierInPlane);
      Assert.Equal(gsaProp2d.BendingStiffnessPercentage.Value, speckleProperty2D.modifierBending);
      Assert.Equal(gsaProp2d.ShearStiffnessPercentage.Value, speckleProperty2D.modifierShear);
      Assert.Equal(gsaProp2d.VolumePercentage.Value, speckleProperty2D.modifierVolume);
    }

    //Property3D not yet supported

    [Fact]
    public void GsaPropMass()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaPropMass = GsaPropMassExample("property mass 1");
      gsaRecords.Add(gsaPropMass);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is PropertyMass);

      var specklePropertyMass = (PropertyMass)structuralObjects.FirstOrDefault(so => so is PropertyMass);

      Assert.Equal("property mass 1", specklePropertyMass.applicationId);
      Assert.Equal(gsaPropMass.Name, specklePropertyMass.name);
      Assert.Equal(gsaPropMass.Mass, specklePropertyMass.mass);
      Assert.Equal(gsaPropMass.Ixx, specklePropertyMass.inertiaXX);
      Assert.Equal(gsaPropMass.Iyy, specklePropertyMass.inertiaYY);
      Assert.Equal(gsaPropMass.Izz, specklePropertyMass.inertiaZZ);
      Assert.Equal(gsaPropMass.Ixy, specklePropertyMass.inertiaXY);
      Assert.Equal(gsaPropMass.Iyz, specklePropertyMass.inertiaYZ);
      Assert.Equal(gsaPropMass.Izx, specklePropertyMass.inertiaZX);
      Assert.True(specklePropertyMass.massModified);
      Assert.Equal(gsaPropMass.ModXPercentage, specklePropertyMass.massModifierX);
      Assert.Equal(gsaPropMass.ModYPercentage, specklePropertyMass.massModifierY);
      Assert.Equal(gsaPropMass.ModZPercentage, specklePropertyMass.massModifierZ);
    }

    [Fact]
    public void GsaPropSpr()
    {
      //Define GSA objects
      //These should be in order that respects the type dependency tree (which is only available in the GSAProxy library, which isn't referenced yet
      var gsaRecords = new List<GsaRecord>();

      //Generation #1: Types with no other dependencies - the leaves of the tree
      var gsaProSpr = GsaPropSprExample("property spring 1");
      gsaRecords.Add(gsaProSpr);

      Instance.GsaModel.Cache.Upsert(gsaRecords);

      foreach (var record in gsaRecords)
      {
        var speckleObjects = converter.ConvertToSpeckle(new List<object> { record });
        Assert.Empty(converter.ConversionErrors);

        Instance.GsaModel.Cache.SetSpeckleObjects(record, speckleObjects.ToDictionary(so => so.applicationId, so => (object)so));
      }

      Assert.True(Instance.GsaModel.Cache.GetSpeckleObjects(out var structuralObjects));

      Assert.NotEmpty(structuralObjects);
      Assert.Contains(structuralObjects, so => so is PropertySpring);

      var specklePropertySpring = (PropertySpring)structuralObjects.FirstOrDefault(so => so is PropertySpring);

      Assert.Equal("property spring 1", specklePropertySpring.applicationId);
      Assert.Equal(gsaProSpr.Name, specklePropertySpring.name);
      Assert.Equal(gsaProSpr.PropertyType.ToString(), specklePropertySpring.springType.ToString());
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.X], specklePropertySpring.stiffnessX);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.Y], specklePropertySpring.stiffnessY);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.Z], specklePropertySpring.stiffnessZ);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.XX], specklePropertySpring.stiffnessXX);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.YY], specklePropertySpring.stiffnessYY);
      Assert.Equal(gsaProSpr.Stiffnesses[AxisDirection6.ZZ], specklePropertySpring.stiffnessZZ);
      Assert.Equal(gsaProSpr.DampingRatio, specklePropertySpring.dampingRatio);
    }
    #endregion

    #region Results
    [Fact]
    public void GsaNodeEmbeddedResults()
    {
    }

    [Fact]
    public void GsaNodeSeparateResults()
    {
    }

    [Fact]
    public void GsaNodeResultsOnly()
    {
    }
    #endregion

    #region helper
    #region Geometry
    private GsaAxis GsaAxisExample(string appId)
    {
      return new GsaAxis()
      {
        Index = 1,
        ApplicationId = appId,
        Name = "1",
        OriginX = 0,
        OriginY = 0,
        OriginZ = 0,
        XDirX = 1 / Math.Sqrt(2),
        XDirY = 1 / Math.Sqrt(2),
        XDirZ = 0,
        XYDirX = -1 / Math.Sqrt(2),
        XYDirY = 1 / Math.Sqrt(2),
        XYDirZ = 0
      };
    }

    private List<GsaEl> GsaElement2dExamples(int numberOfElements, params string[] appIds)
    {
      var gsaEl = new List<GsaEl> {
        new GsaEl()
        {
          Index = 1,
          Name = "",
          Colour = Colour.NO_RGB,
          Type = ElementType.Quad4,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>() { 1, 2, 3, 4 },
          OrientationNodeIndex = 0,
          Angle = 0,
          ReleaseInclusion = ReleaseInclusion.NotIncluded,
          OffsetZ = 0,
          ParentIndex = 1
        },
        new GsaEl()
        {
          Index = 2,
          Name = "",
          Colour = Colour.NO_RGB,
          Type = ElementType.Triangle3,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>() { 1, 2, 5 },
          OrientationNodeIndex = 0,
          Angle = 0,
          ReleaseInclusion = ReleaseInclusion.NotIncluded,
          OffsetZ = 0,
          ParentIndex = 1
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaEl[i].ApplicationId = appIds[i];
      }
      return gsaEl.GetRange(0, numberOfElements);
    }

    private List<GsaEl> GsaElement1dExamples(int numberOfElements, params string[] appIds)
    {
      var gsaEl = new List<GsaEl>()
      {
        new GsaEl()
        {
          Index = 1,
          Name = "1",
          Colour = Colour.NO_RGB,
          Type = ElementType.Beam,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>() { 1, 2 },
          ReleaseInclusion = ReleaseInclusion.NotIncluded,
          OffsetY = 0,
          OffsetZ = 0,
          ParentIndex = 1
        },
        new GsaEl()
        {
          Index = 2,
          Name = "2",
          Colour = Colour.NO_RGB,
          Type = ElementType.Beam,
          PropertyIndex = 1,
          Group = 1,
          NodeIndices = new List<int>() { 2, 3 },
          Angle = 90,
          ReleaseInclusion = ReleaseInclusion.NotIncluded,
          OffsetY = 0,
          OffsetZ = 0,
          ParentIndex = 1
        }
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaEl[i].ApplicationId = appIds[i];
      }
      return gsaEl.GetRange(0, numberOfElements);
    }

    private List<GsaNode> GsaNodeExamples(int numberOfNodes, params string[] appIds)
    {
      var gsaNodes = new List<GsaNode>
      {
        new GsaNode()
        {
          Name = "1",
          Index = 1,
          Colour = Colour.NO_RGB,
          X = 0,
          Y = 0,
          Z = 0,
          NodeRestraint = NodeRestraint.Pin,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
          SpringPropertyIndex = 1,
          MassPropertyIndex = 1
        },
        new GsaNode()
        {
          Name = "2",
          Index = 2,
          Colour = Colour.NO_RGB,
          X = 1,
          Y = 0,
          Z = 0,
          NodeRestraint = NodeRestraint.Free,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
          SpringPropertyIndex = 1,
          MassPropertyIndex = 1
        },
        new GsaNode()
        {
          Name = "3",
          Index = 3,
          Colour = Colour.NO_RGB,
          X = 1,
          Y = 1,
          Z = 0,
          NodeRestraint = NodeRestraint.Free,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
          SpringPropertyIndex = 1,
          MassPropertyIndex = 1
        },
        new GsaNode()
        {
          Name = "4",
          Index = 4,
          Colour = Colour.NO_RGB,
          X = 0,
          Y = 1,
          Z = 0,
          NodeRestraint = NodeRestraint.Free,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
          SpringPropertyIndex = 1,
          MassPropertyIndex = 1
        },
        new GsaNode()
        {
          Name = "5",
          Index = 5,
          Colour = Colour.NO_RGB,
          X = 2,
          Y = 0,
          Z = 0,
          NodeRestraint = NodeRestraint.Free,
          AxisRefType = NodeAxisRefType.Global,
          MeshSize = 1,
          SpringPropertyIndex = 1,
          MassPropertyIndex = 1
        },
      };
      for (int i = 0; i < appIds.Count(); i++)
      {
        gsaNodes[i].ApplicationId = appIds[i];
      }
      return gsaNodes.GetRange(0, numberOfNodes);
    }
    #endregion

    #region Loading
    #endregion

    #region Materials
    private GsaMatConcrete GsaMatConcreteExample(string appId)
    {
      return new GsaMatConcrete()
      {
        Index = 1,
        ApplicationId = appId,
        Name = "1",
        Mat = new GsaMat()
        {
          Name = "",
          E = 3.315274903e+10,
          F = 40000000,
          Nu = 0.2,
          G = 1.381364543e+10,
          Rho = 2400,
          Alpha = 1e-5,
          Prop = new GsaMatAnal()
          {
            Type = MatAnalType.MAT_ELAS_ISO,
            NumParams = 6,
            E = 3.315274903e+10,
            Nu = 0.2,
            Rho = 2400,
            Alpha = 1e-5,
            G = 1.381364543e+10,
            Damp = 0
          },
          NumUC = 0,
          AbsUC = Dimension.NotSet,
          OrdUC = Dimension.NotSet,
          PtsUC = new double[0],
          NumSC = 0,
          AbsSC = Dimension.NotSet,
          OrdSC = Dimension.NotSet,
          PtsSC = new double[0],
          NumUT = 0,
          AbsUT = Dimension.NotSet,
          OrdUT = Dimension.NotSet,
          PtsUT = new double[0],
          NumST = 0,
          AbsST = Dimension.NotSet,
          OrdST = Dimension.NotSet,
          PtsST = new double[0],
          Eps = 0,
          Uls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.RECTANGLE, MatCurveParamType.NO_TENSION },
            StrainElasticCompression = 0.00068931,
            StrainElasticTension = 0,
            StrainPlasticCompression = 0.00069069,
            StrainPlasticTension = 0,
            StrainFailureCompression = 0.003,
            StrainFailureTension = 1,
            GammaF = 1,
            GammaE = 1
          },
          Sls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.LINEAR, MatCurveParamType.INTERPOLATED },
            StrainElasticCompression = 0.003,
            StrainElasticTension = 0,
            StrainPlasticCompression = 0.003,
            StrainPlasticTension = 0,
            StrainFailureCompression = 0.003,
            StrainFailureTension = 0.0001144620975,
            GammaF = 1,
            GammaE = 1
          },
          Cost = 0,
          Type = MatType.CONCRETE
        },
        Type = MatConcreteType.CYLINDER,
        Cement = MatConcreteCement.N,
        Fc = 40000000,
        Fcd = 34000000,
        Fcdc = 16000000,
        Fcdt = 3794733.192,
        Fcfib = 2276839.915,
        EmEs = 0,
        N = 2,
        Emod = 1,
        EpsPeak = 0.003,
        EpsMax = 0.00069,
        EpsU = 0.003,
        EpsAx = 0.0025,
        EpsTran = 0.002,
        EpsAxs = 0.0025,
        Light = false,
        Agg = 0.02,
        XdMin = 0,
        XdMax = 1,
        Beta = 0.77,
        Shrink = 0,
        Confine = 0,
        Fcc = 0,
        EpsPlasC = 0,
        EpsUC = 0
      };
    }

    private GsaMatSteel GsaMatSteelExample(string appId)
    {
      return new GsaMatSteel()
      {
        Index = 1,
        ApplicationId = appId,
        Name = "1",
        Mat = new GsaMat()
        {
          Name = "",
          E = 2e11,
          F = 360000000,
          Nu = 0.3,
          G = 7.692307692e+10,
          Rho = 7850,
          Alpha = 1.2e-5,
          Prop = new GsaMatAnal()
          {
            Type = MatAnalType.MAT_ELAS_ISO,
            NumParams = 6,
            E = 2e11,
            Nu = 0.3,
            Rho = 7850,
            Alpha = 1.2e-5,
            G = 7.692307692e+10,
            Damp = 0
          },
          NumUC = 0,
          AbsUC = Dimension.NotSet,
          OrdUC = Dimension.NotSet,
          PtsUC = new double[0],
          NumSC = 0,
          AbsSC = Dimension.NotSet,
          OrdSC = Dimension.NotSet,
          PtsSC = new double[0],
          NumUT = 0,
          AbsUT = Dimension.NotSet,
          OrdUT = Dimension.NotSet,
          PtsUT = new double[0],
          NumST = 0,
          AbsST = Dimension.NotSet,
          OrdST = Dimension.NotSet,
          PtsST = new double[0],
          Eps = 0.05,
          Uls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.UNDEF },
            StrainElasticCompression = 0.0018,
            StrainElasticTension = 0.0018,
            StrainPlasticCompression = 0.0018,
            StrainPlasticTension = 0.0018,
            StrainFailureCompression = 0.05,
            StrainFailureTension = 0.05,
            GammaF = 1,
            GammaE = 1
          },
          Sls = new GsaMatCurveParam()
          {
            Model = new List<MatCurveParamType>() { MatCurveParamType.ELAS_PLAS },
            StrainElasticCompression = 0.0018,
            StrainElasticTension = 0.0018,
            StrainPlasticCompression = 0.0018,
            StrainPlasticTension = 0.0018,
            StrainFailureCompression = 0.05,
            StrainFailureTension = 0.05,
            GammaF = 1,
            GammaE = 1
          },
          Cost = 0,
          Type = MatType.STEEL
        },
        Fy = 360000000,
        Fu = 450000000,
        EpsP = 0,
        Eh = 0,
      };
    }
    #endregion

    #region Properties
    private GsaProp2d GsaProp2dExample(string appId)
    {
      return new GsaProp2d()
      {
        Index = 1,
        Name = "1",
        ApplicationId = appId,
        Colour = Colour.NO_RGB,
        Type = Property2dType.Shell,
        AxisRefType = AxisRefType.Global,
        AnalysisMaterialIndex = 0,
        MatType = Property2dMaterialType.Steel,
        GradeIndex = 1,
        DesignIndex = 0,
        Thickness = 0.01,
        RefPt = Property2dRefSurface.Centroid,
        RefZ = 0,
        Mass = 0,
        BendingStiffnessPercentage = 1,
        ShearStiffnessPercentage = 1,
        InPlaneStiffnessPercentage = 1,
        VolumePercentage = 1
      };
    }

    private GsaSection GsaCatalogueSectionExample(string appId)
    {
      return new GsaSection()
      {
        Index = 1,
        Name = "1",
        ApplicationId = appId,
        Colour = Colour.NO_RGB,
        Type = Section1dType.Generic,
        //PoolIndex = 0,
        ReferencePoint = ReferencePoint.Centroid,
        RefY = 0,
        RefZ = 0,
        Mass = 0,
        Fraction = 1,
        Cost = 0,
        Left = 0,
        Right = 0,
        Slab = 0,
        Components = new List<GsaSectionComponentBase>()
        {
          new SectionComp()
          {
            Name = "",
            //MatAnalIndex = 0,
            MaterialType = Section1dMaterialType.STEEL,
            MaterialIndex = 1,
            OffsetY = 0,
            OffsetZ = 0,
            Rotation = 0,
            Reflect = ComponentReflection.NONE,
            //Pool = 0,
            TaperType = Section1dTaperType.NONE,
            //TaperPos = 0
            ProfileGroup = Section1dProfileGroup.Catalogue,
            ProfileDetails = new ProfileDetailsCatalogue()
            {
              Group = Section1dProfileGroup.Catalogue,
              Profile = "CAT A-UB 610UB125 19981201"
            }
          },
          new SectionSteel()
          {
            //GradeIndex = 0,
            PlasElas = 1,
            NetGross = 1,
            Exposed = 1,
            Beta = 0.4,
            Type = SectionSteelSectionType.HotRolled,
            Plate = SectionSteelPlateType.Undefined,
            Locked = false
          }
        },
        Environ = false
      };
    }

    private GsaPropMass GsaPropMassExample(string appId)
    {
      return new GsaPropMass()
      {
        Index = 1,
        ApplicationId = appId,
        Mass = 10,
        Ixx = 0,
        Iyy = 0,
        Izz = 0,
        Ixy = 0,
        Iyz = 0,
        Izx = 0,
        Mod = MassModification.Modified,
        ModXPercentage = 1,
        ModYPercentage = 1,
        ModZPercentage = 1
      };
    }

    private GsaPropSpr GsaPropSprExample(string appId)
    {
      return new GsaPropSpr()
      {
        Index = 1,
        ApplicationId = appId,
        Name = "1",
        Colour = Colour.NO_RGB,
        PropertyType = StructuralSpringPropertyType.General,
        Stiffnesses = new Dictionary<AxisDirection6, double>
        { { AxisDirection6.X, 10 },
          { AxisDirection6.Y, 11 },
          { AxisDirection6.Z, 12 },
          { AxisDirection6.XX, 13 },
          { AxisDirection6.YY, 14 },
          { AxisDirection6.ZZ, 15 },
        },
        DampingRatio = 5
      };
    }
    #endregion

    #region Results
    #endregion
    #endregion
  }
}
