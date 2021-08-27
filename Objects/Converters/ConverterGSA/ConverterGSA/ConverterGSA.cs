﻿using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.Properties;
using Objects.Structural;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using Restraint = Objects.Structural.Geometry.Restraint;
using Objects.Structural.Materials;
using MemberType = Objects.Structural.Geometry.MemberType;
using System.Runtime.InteropServices;

namespace ConverterGSA
{
  public class ConverterGSA : ISpeckleConverter
  {
    #region ISpeckleConverter props
    public static string AppName = Applications.GSA;
    public string Description => "Default Speckle Kit for GSA";

    public string Name => nameof(ConverterGSA);

    public string Author => "Arup";

    public string WebsiteOrEmail => "https://www.oasys-software.com/";

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();
    #endregion ISpeckleConverter props

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public Dictionary<Type, Func<GsaRecord, List<Base>>> ToSpeckleFns;
    public Dictionary<Type, Func<Base, List<GsaRecord>>> ToNativeFns;

    public ConverterGSA()
    {
      ToSpeckleFns = new Dictionary<Type, Func<GsaRecord, List<Base>>>()
        {
          //Geometry
          { typeof(GsaAxis), GsaAxisToSpeckle },
          { typeof(GsaNode), GsaNodeToSpeckle },
          { typeof(GsaEl), GsaElementToSpeckle },
          //Loading

          //Material
          { typeof(GsaMatSteel), GsaMaterialSteelToSpeckle },
          { typeof(GsaMatConcrete), GsaMaterialConcreteToSpeckle },
          //Property
          { typeof(GsaSection), GsaSectionToSpeckle },
          { typeof(GsaProp2d), GsaProperty2dToSpeckle },
          //{ typeof(GsaProp3d), GsaProperty3dToSpeckle }, not supported yet
          { typeof(GsaPropMass), GsaPropertyMassToSpeckle },
          { typeof(GsaPropSpr), GsaPropertySpringToSpeckle },
          //Result

          //TODO: add methods for other GSA keywords
        };

      ToNativeFns = new Dictionary<Type, Func<Base, List<GsaRecord>>>()
      {
        {  typeof(Axis), AxisToNative }
      };
    }

    public bool CanConvertToNative(Base @object)
    {
      var t = @object.GetType();
      return ToNativeFns.ContainsKey(t);
    }

    public bool CanConvertToSpeckle(object @object)
    {
      var t = @object.GetType();
      return (t.IsSubclassOf(typeof(GsaRecord)) && ToSpeckleFns.ContainsKey(t));
    }

    public object ConvertToNative(Base @object)
    {
      var t = @object.GetType();
      return ToNativeFns[t](@object);
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      var retList = new List<object>();
      foreach (var obj in objects)
      {
        var natives = ConvertToNative(obj);
        if (natives != null)
        {
          if (natives is List<GsaRecord>)
          {
            retList.AddRange(((List<GsaRecord>)natives).Cast<object>());
          }
        }
      }
      return retList;
    }

    public Base ConvertToSpeckle(object @object)
    {
      if (@object is List<GsaRecord>)
      {
        //by calling this method with List<GsaRecord>, it is assumed that either:
        //- the caller doesn't care about retrieving any Speckle objects, since a conversion could result in multiple and this method only gives back the first
        //- the caller expects the conversion to only result in one Speckle object anyway
        var objects = ConvertToSpeckle(((List<GsaRecord>)@object).Cast<object>().ToList());
        return objects.First();
      }
      throw new NotImplementedException();
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      var native = objects.Where(o => o.GetType().IsSubclassOf(typeof(GsaRecord)));
      if (native.Count() < objects.Count())
      {
        ConversionErrors.Add(new Exception("Non-native objects: " + (objects.Count() - native.Count())));
        objects = native.ToList();
      }
      var retList = new List<Base>();
      foreach (var x in objects)
      {
        var speckleObjects = ToSpeckle((GsaRecord)x);
        if (speckleObjects != null && speckleObjects.Count > 0)
        {
          retList.AddRange(speckleObjects.Where(so => so != null));
        }
      }
      return retList;
    }

    public IEnumerable<string> GetServicedApplications() => new string[] { AppName };

    public void SetContextDocument(object doc)
    {
      throw new NotImplementedException();
    }

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects)
    {
      throw new NotImplementedException();
    }

    #region ToSpeckle
    private List<Base> ToSpeckle(GsaRecord nativeObject)
    {
      var nativeType = nativeObject.GetType();
      return ToSpeckleFns.ContainsKey(nativeType) ? ToSpeckleFns[nativeType](nativeObject) : null;
    }

    #region Geometry
    public List<Base> GsaNodeToSpeckle(GsaRecord nativeObject)
    {
      var node = GsaNodeToSpeckle((GsaNode)nativeObject);
      return new List<Base>() { node };
    }

    public Node GsaNodeToSpeckle(GsaNode gsaNode, string units = null)
    {
      //Node specific members
      var speckleNode = new Node()
      {
        name = gsaNode.Name,
        basePoint = new Point(gsaNode.X, gsaNode.Y, gsaNode.Z, units),
        constraintAxis = GetConstraintAxis(gsaNode),
        restraint = GetRestraint(gsaNode)
      };
      if (IsIndex(gsaNode.Index))
      {
        speckleNode.applicationId = Instance.GsaModel.GetApplicationId<GsaNode>(gsaNode.Index.Value);
      }

      //Dynamic properties (TO DO: update Schema)
      speckleNode["colour"] = gsaNode.Colour.ToString();

      if (IsPositive(gsaNode.MeshSize))
        speckleNode["localElementSize"] = gsaNode.MeshSize.Value;

      if (IsIndex(gsaNode.MassPropertyIndex))
        speckleNode["propertyMass"] = GetPropertyMassFromIndex(gsaNode.MassPropertyIndex.Value);

      if (IsIndex(gsaNode.SpringPropertyIndex))
        speckleNode["propertySpring"] = GetPropertySpringFromIndex(gsaNode.SpringPropertyIndex.Value);

      return speckleNode;
    }

    public List<Base> GsaAxisToSpeckle(GsaRecord nativeObject)
    {
      var axis = GsaAxisToSpeckle((GsaAxis)nativeObject);
      return new List<Base>() { axis };
    }

    public Axis GsaAxisToSpeckle(GsaAxis gsaAxis)
    {
      //Only supporting cartesian coordinate systems at the moment
      var speckleAxis = new Axis()
      {
        name = gsaAxis.Name,
        axisType = AxisType.Cartesian,
      };
      if (IsIndex(gsaAxis.Index))
      {
        speckleAxis.applicationId = Instance.GsaModel.GetApplicationId<GsaAxis>(gsaAxis.Index.Value);
      }

      if (gsaAxis.XDirX.HasValue && gsaAxis.XDirY.HasValue && gsaAxis.XDirZ.HasValue && gsaAxis.XYDirX.HasValue && gsaAxis.XYDirY.HasValue && gsaAxis.XYDirZ.HasValue)
      {
        var origin = new Point(gsaAxis.OriginX, gsaAxis.OriginY, gsaAxis.OriginZ);
        var xdir = Vector.UnitVector(new Vector(gsaAxis.XDirX.Value, gsaAxis.XDirY.Value, gsaAxis.XDirZ.Value));
        var ydir = Vector.UnitVector(new Vector(gsaAxis.XYDirX.Value, gsaAxis.XYDirY.Value, gsaAxis.XYDirZ.Value));
        var normal = Vector.UnitVector(xdir * ydir);
        ydir = -Vector.UnitVector(xdir * normal);
        speckleAxis.definition = new Plane(origin, normal, xdir, ydir);
      }
      else
      {
        speckleAxis.definition = GlobalAxis().definition;
      }

      return speckleAxis;
    }

    public List<Base> GsaElementToSpeckle(GsaRecord nativeObject)
    {
      var element = GsaElementToSpeckle((GsaEl)nativeObject);
      return new List<Base>() { element };
    }

    public Base GsaElementToSpeckle(GsaEl gsaEl)
    {
      if (Is3dElement(gsaEl)) //3D element
      {
        return GsaElement3dToSpeckle(gsaEl);
      }
      else if (Is2dElement(gsaEl)) // 2D element
      {
        return GsaElement2dToSpeckle(gsaEl);
      }
      else //1D element
      {
        return GsaElement1dToSpeckle(gsaEl);
      }
    }

    public Element1D GsaElement1dToSpeckle(GsaEl gsaEl)
    {
      var speckleElement1d = new Element1D()
      {
        name = gsaEl.Name,
        type = GetElement1dType(gsaEl.Type),
        end1Releases = GetRestraint(gsaEl.Releases1),
        end2Releases = GetRestraint(gsaEl.Releases2),
        end1Offset = new Vector(),
        end2Offset = new Vector(),
        orientationAngle = 0, //default
        parent = new Base(), //TO DO: add parent
        end1Node = GetNodeFromIndex(gsaEl.NodeIndices[0]),
        end2Node = GetNodeFromIndex(gsaEl.NodeIndices[1]),
        topology = new List<Node>(),
        displayMesh = new Mesh() //TO DO: add display mesh
      };

      if (IsIndex(gsaEl.Index))
      {
        speckleElement1d.applicationId = Instance.GsaModel.GetApplicationId<GsaEl>(gsaEl.Index.Value);
      }

      //Section
      if (gsaEl.PropertyIndex.HasValue) speckleElement1d.property = GetProperty1dFromIndex(gsaEl.PropertyIndex.Value);

      //Nodes
      if (gsaEl.Angle.HasValue) speckleElement1d.orientationAngle = gsaEl.Angle.Value;
      if (gsaEl.OrientationNodeIndex.HasValue) speckleElement1d.orientationNode = GetNodeFromIndex(gsaEl.OrientationNodeIndex.Value);
      foreach (var index in gsaEl.NodeIndices) speckleElement1d.topology.Add(GetNodeFromIndex(index));

      //Local Axis
      speckleElement1d.localAxis = GetLocalAxis(speckleElement1d.end1Node, speckleElement1d.end2Node, speckleElement1d.orientationNode, Radians(speckleElement1d.orientationAngle));

      //Offsets
      if (gsaEl.End1OffsetX.HasValue) speckleElement1d.end1Offset.x = gsaEl.End1OffsetX.Value;
      if (gsaEl.OffsetY.HasValue) speckleElement1d.end1Offset.y = gsaEl.OffsetY.Value;
      if (gsaEl.OffsetZ.HasValue) speckleElement1d.end1Offset.z = gsaEl.OffsetZ.Value;
      if (gsaEl.End2OffsetX.HasValue) speckleElement1d.end2Offset.x = gsaEl.End2OffsetX.Value;
      if (gsaEl.OffsetY.HasValue) speckleElement1d.end2Offset.y = gsaEl.OffsetY.Value;
      if (gsaEl.OffsetZ.HasValue) speckleElement1d.end2Offset.z = gsaEl.OffsetZ.Value;

      return speckleElement1d;
    }

    public Element2D GsaElement2dToSpeckle(GsaEl gsaEl)
    {
      var speckleElement2d = new Element2D()
      {
        name = gsaEl.Name,
        type = (ElementType2D)Enum.Parse(typeof(ElementType2D), gsaEl.Type.ToString()),
        parent = new Base(), //TO DO: add parent
        displayMesh = new Mesh(), //TO DO: add display mesh
        baseMesh = new Mesh() //TO DO: add base mesh
      };

      if (IsIndex(gsaEl.Index))
      {
        speckleElement2d.applicationId = Instance.GsaModel.GetApplicationId<GsaEl>(gsaEl.Index.Value);
      }
      if (IsIndex(gsaEl.PropertyIndex)) speckleElement2d.property = GetProperty2dFromIndex(gsaEl.PropertyIndex.Value);
      if (gsaEl.OffsetZ.HasValue) speckleElement2d.offset = gsaEl.OffsetZ.Value;
      if (gsaEl.Angle.HasValue) speckleElement2d.orientationAngle = gsaEl.Angle.Value;
      speckleElement2d.topology = gsaEl.NodeIndices.Select(i => GetNodeFromIndex(i)).ToList();

      return speckleElement2d;
    }

    public Element3D GsaElement3dToSpeckle(GsaEl gsaEl)
    {
      //TODO
      return new Element3D();
    }
    #endregion

    #region Loading
    //TODO: implement conversion code for loading objects
    /* AreaLoad
     * BeamLoad
     * FaceLoad
     * GravityLoad
     * LoadCase
     * LoadCombination
     * NodeLoad
     */
    #endregion

    #region Materials
    public List<Base> GsaMaterialSteelToSpeckle(GsaRecord nativeObject)
    {
      var steel = GsaMaterialSteelToSpeckle((GsaMatSteel)nativeObject);
      return new List<Base>() { steel };
    }

    public Steel GsaMaterialSteelToSpeckle(GsaMatSteel gsaMatSteel)
    {
      //Currently only handles isotropic steel properties.
      //A lot of information in the gsa objects are currently ignored.

      //Gwa keyword SPEC_STEEL_DESIGN is not well documented:
      //
      //SPEC_STEEL_DESIGN | code
      //
      //Description
      //  Steel design code
      //
      //Parameters
      //  code      steel design code
      //
      //Example (GSA 10.1)
      //  SPEC_STEEL_DESIGN.1	AS 4100-1998	YES	15	YES	15	15	YES	NO	NO	NO
      //
      var speckleSteel = new Steel()
      {
        name = gsaMatSteel.Name,
        grade = "",                                 //grade can be determined from gsaMatSteel.Mat.Name (assuming the user doesn't change the default value): e.g. "350(AS3678)"
        type = MaterialType.Steel,
        designCode = "",                            //designCode can be determined from SPEC_STEEL_DESIGN gwa keyword
        codeYear = "",                              //codeYear can be determined from SPEC_STEEL_DESIGN gwa keyword
        yieldStrength = gsaMatSteel.Fy.Value,
        ultimateStrength = gsaMatSteel.Fu.Value,
        maxStrain = gsaMatSteel.EpsP.Value
      };
      if (IsIndex(gsaMatSteel.Index))
      {
        speckleSteel.applicationId = Instance.GsaModel.GetApplicationId<GsaMatSteel>(gsaMatSteel.Index.Value);
      }

      //the following properties are stored in multiple locations in GSA
      if (Choose(gsaMatSteel.Mat.E, gsaMatSteel.Mat.Prop.E, out var E)) speckleSteel.youngsModulus = E;
      if (Choose(gsaMatSteel.Mat.Nu, gsaMatSteel.Mat.Prop.Nu, out var Nu)) speckleSteel.poissonsRatio = Nu;
      if (Choose(gsaMatSteel.Mat.G, gsaMatSteel.Mat.Prop.G, out var G)) speckleSteel.shearModulus = G;
      if (Choose(gsaMatSteel.Mat.Rho, gsaMatSteel.Mat.Prop.Rho, out var Rho)) speckleSteel.density = Rho;
      if (Choose(gsaMatSteel.Mat.Alpha, gsaMatSteel.Mat.Prop.Alpha, out var Alpha)) speckleSteel.thermalExpansivity = Alpha;

      return speckleSteel;
    }

    public List<Base> GsaMaterialConcreteToSpeckle(GsaRecord nativeObject)
    {
      var concrete = GsaMaterialConcreteToSpeckle((GsaMatConcrete)nativeObject);
      return new List<Base>() { concrete };
    }

    public Concrete GsaMaterialConcreteToSpeckle(GsaMatConcrete gsaMatConcrete)
    {
      //Currently only handles isotropic concrete properties.
      //A lot of information in the gsa objects are currently ignored.

      var speckleConcrete = new Concrete()
      {
        name = gsaMatConcrete.Name,
        grade = "",                                 //grade can be determined from gsaMatConcrete.Mat.Name (assuming the user doesn't change the default value): e.g. "32 MPa"
        type = MaterialType.Concrete,
        designCode = "",                            //designCode can be determined from SPEC_CONCRETE_DESIGN gwa keyword: e.g. "AS3600_18" -> "AS3600"
        codeYear = "",                              //codeYear can be determined from SPEC_CONCRETE_DESIGN gwa keyword: e.g. "AS3600_18" - "2018"
        flexuralStrength = 0
      };
      if (IsIndex(gsaMatConcrete.Index)) speckleConcrete.applicationId = Instance.GsaModel.GetApplicationId<GsaMatConcrete>(gsaMatConcrete.Index.Value);

      //the following properties might be null
      if (gsaMatConcrete.Fc.HasValue) speckleConcrete.compressiveStrength = gsaMatConcrete.Fc.Value;
      if (gsaMatConcrete.EpsU.HasValue) speckleConcrete.maxStrain = gsaMatConcrete.EpsU.Value;
      if (gsaMatConcrete.Agg.HasValue) speckleConcrete.maxAggregateSize = gsaMatConcrete.Agg.Value;
      if (gsaMatConcrete.Fcdt.HasValue) speckleConcrete.tensileStrength = gsaMatConcrete.Fcdt.Value;

      //the following properties are stored in multiple locations in GSA
      if (Choose(gsaMatConcrete.Mat.E, gsaMatConcrete.Mat.Prop.E, out var E)) speckleConcrete.youngsModulus = E;
      if (Choose(gsaMatConcrete.Mat.Nu, gsaMatConcrete.Mat.Prop.Nu, out var Nu)) speckleConcrete.poissonsRatio = Nu;
      if (Choose(gsaMatConcrete.Mat.G, gsaMatConcrete.Mat.Prop.G, out var G)) speckleConcrete.shearModulus = G;
      if (Choose(gsaMatConcrete.Mat.Rho, gsaMatConcrete.Mat.Prop.Rho, out var Rho)) speckleConcrete.density = Rho;
      if (Choose(gsaMatConcrete.Mat.Alpha, gsaMatConcrete.Mat.Prop.Alpha, out var Alpha)) speckleConcrete.thermalExpansivity = Alpha;

      return speckleConcrete;
    }

    //Timber: GSA keyword not supported yet
    #endregion

    #region Property
    public List<Base> GsaSectionToSpeckle(GsaRecord nativeObject)
    {
      var section = GsaSectionToSpeckle((GsaSection)nativeObject);
      return new List<Base>() { section };
    }

    public Property1D GsaSectionToSpeckle(GsaSection gsaSection)
    {
      //TO DO: update code to handle modifiers once SECTION_MOD (or SECTION_ANAL) keyword is supported
      var speckleProperty1D = new Property1D()
      {
        name = gsaSection.Name,
        colour = gsaSection.Colour.ToString(),
        memberType = MemberType.Generic1D,
        grade = "", // TO DO: what is grade used for?
        referencePoint = GetReferencePoint(gsaSection.ReferencePoint),
      };

      if (IsIndex(gsaSection.Index)) speckleProperty1D.applicationId = Instance.GsaModel.GetApplicationId<GsaSection>(gsaSection.Index.Value);
      if (gsaSection.RefY.HasValue) speckleProperty1D.offsetY = gsaSection.RefY.Value;
      if (gsaSection.RefZ.HasValue) speckleProperty1D.offsetZ = gsaSection.RefZ.Value;

      var gsaSectionComp = (SectionComp)gsaSection.Components.Find(x => x.GetType() == typeof(SectionComp));
      speckleProperty1D.profile = GetProfile(gsaSectionComp.ProfileDetails);
      if (gsaSectionComp.MaterialIndex.HasValue)
      {
        speckleProperty1D.material = GetMaterialFromIndex(gsaSectionComp.MaterialIndex.Value, gsaSectionComp.MaterialType);
      }
      if (gsaSectionComp.ProfileGroup == Section1dProfileGroup.Explicit)
      {
        var gsaProfile = (ProfileDetailsExplicit)gsaSectionComp.ProfileDetails;
        if (gsaProfile.Area.HasValue) speckleProperty1D.area = gsaProfile.Area.Value;
        if (gsaProfile.Iyy.HasValue) speckleProperty1D.Iyy = gsaProfile.Iyy.Value;
        if (gsaProfile.Izz.HasValue) speckleProperty1D.Izz = gsaProfile.Izz.Value;
        if (gsaProfile.J.HasValue) speckleProperty1D.J = gsaProfile.J.Value;
        if (gsaProfile.Ky.HasValue) speckleProperty1D.Ky = gsaProfile.Ky.Value;
        if (gsaProfile.Kz.HasValue) speckleProperty1D.Kz = gsaProfile.Kz.Value;
      }

      return speckleProperty1D;
    }

    public List<Base> GsaProperty2dToSpeckle(GsaRecord nativeObject)
    {
      var prop2d = GsaProperty2dToSpeckle((GsaProp2d)nativeObject);
      return new List<Base>() { prop2d };
    }

    public Property2D GsaProperty2dToSpeckle(GsaProp2d gsaProp2d)
    {
      var speckleProperty2D = new Property2D()
      {
        name = gsaProp2d.Name,
        colour = gsaProp2d.Colour.ToString(),
        zOffset = gsaProp2d.RefZ,
        grade = "", // TO DO: what is grade used for?
        orientationAxis = GetOrientationAxis(gsaProp2d),
        refSurface = GetReferenceSurface(gsaProp2d)
      };

      if (IsIndex(gsaProp2d.Index))
      {
        speckleProperty2D.applicationId = Instance.GsaModel.GetApplicationId<GsaProp2d>(gsaProp2d.Index.Value);
      }
      if (IsPositive(gsaProp2d.Thickness)) speckleProperty2D.thickness = gsaProp2d.Thickness.Value;
      if (IsIndex(gsaProp2d.GradeIndex)) speckleProperty2D.material = GetMaterialFromIndex(gsaProp2d.GradeIndex.Value, gsaProp2d.MatType);
      if (gsaProp2d.Type != Property2dType.NotSet) speckleProperty2D.type = (PropertyType2D)Enum.Parse(typeof(PropertyType2D), gsaProp2d.Type.ToString());

      //Only supporting Percentage modifiers
      if (gsaProp2d.InPlaneStiffnessPercentage.HasValue) speckleProperty2D.modifierInPlane = gsaProp2d.InPlaneStiffnessPercentage.Value;
      if (gsaProp2d.BendingStiffnessPercentage.HasValue) speckleProperty2D.modifierBending = gsaProp2d.BendingStiffnessPercentage.Value;
      if (gsaProp2d.ShearStiffnessPercentage.HasValue) speckleProperty2D.modifierShear = gsaProp2d.ShearStiffnessPercentage.Value;
      if (gsaProp2d.VolumePercentage.HasValue) speckleProperty2D.modifierVolume = gsaProp2d.VolumePercentage.Value;

      return speckleProperty2D;
    }

    //Property3D: GSA keyword not supported yet

    public List<Base> GsaPropertyMassToSpeckle(GsaRecord nativeObject)
    {
      var propMass = GsaPropertyMassToSpeckle((GsaPropMass)nativeObject);
      return new List<Base>() { propMass };
    }

    public PropertyMass GsaPropertyMassToSpeckle(GsaPropMass gsaPropMass)
    {
      var specklePropertyMass = new PropertyMass()
      {
        name = gsaPropMass.Name,
        mass = gsaPropMass.Mass,
        inertiaXX = gsaPropMass.Ixx,
        inertiaYY = gsaPropMass.Iyy,
        inertiaZZ = gsaPropMass.Izz,
        inertiaXY = gsaPropMass.Ixy,
        inertiaYZ = gsaPropMass.Iyz,
        inertiaZX = gsaPropMass.Izx
      };
      if (IsIndex(gsaPropMass.Index))
      {
        specklePropertyMass.applicationId = Instance.GsaModel.GetApplicationId<GsaPropMass>(gsaPropMass.Index.Value);
      }

      //Mass modifications
      if (gsaPropMass.Mod == MassModification.Modified)
      {
        specklePropertyMass.massModified = true;
        if (IsPositive(gsaPropMass.ModXPercentage)) specklePropertyMass.massModifierX = gsaPropMass.ModXPercentage.Value;
        if (IsPositive(gsaPropMass.ModYPercentage)) specklePropertyMass.massModifierY = gsaPropMass.ModYPercentage.Value;
        if (IsPositive(gsaPropMass.ModZPercentage)) specklePropertyMass.massModifierZ = gsaPropMass.ModZPercentage.Value;
      }
      else
      {
        specklePropertyMass.massModified = false;
      }

      return specklePropertyMass;
    }

    public List<Base> GsaPropertySpringToSpeckle(GsaRecord nativeObject)
    {
      var propSpring = GsaPropertySpringToSpeckle((GsaPropSpr)nativeObject);
      return new List<Base>() { propSpring };
    }

    public PropertySpring GsaPropertySpringToSpeckle(GsaPropSpr gsaPropSpr)
    {
      //Apply properties common to all spring types
      var specklePropertySpring = new PropertySpring()
      {
        name = gsaPropSpr.Name,
        dampingRatio = gsaPropSpr.DampingRatio.Value
      };
      if (IsIndex(gsaPropSpr.Index))
      {
        specklePropertySpring.applicationId = Instance.GsaModel.GetApplicationId<GsaPropSpr>(gsaPropSpr.Index.Value);
      }

      //Dictionary of fns used to apply spring type specific properties. 
      //Functions will pass by reference specklePropertySpring and make the necessary changes to it
      var fns = new Dictionary<StructuralSpringPropertyType, Func<GsaPropSpr, PropertySpring, bool>>
      { { StructuralSpringPropertyType.Axial, SetProprtySpringAxial },
        { StructuralSpringPropertyType.Torsional, SetPropertySpringTorsional },
        { StructuralSpringPropertyType.Compression, SetProprtySpringCompression },
        { StructuralSpringPropertyType.Tension, SetProprtySpringTension },
        { StructuralSpringPropertyType.Lockup, SetProprtySpringLockup },
        { StructuralSpringPropertyType.Gap, SetProprtySpringGap },
        { StructuralSpringPropertyType.Friction, SetProprtySpringFriction },
        { StructuralSpringPropertyType.General, SetProprtySpringGeneral }
        //MATRIX not yet supported
        //CONNECT not yet supported
      };

      //Apply spring type specific properties
      if (fns.ContainsKey(gsaPropSpr.PropertyType))
      {
        fns[gsaPropSpr.PropertyType](gsaPropSpr, specklePropertySpring);
      }

      return specklePropertySpring;
    }

    //PropertyDamper: GSA keyword not supported yet
    #endregion

    #region Results
    //TODO: implement conversion code for result objects
    /* Result1D
     * Result2D
     * Result3D
     * ResultGlobal
     * ResultNode
     */
    #endregion
    #endregion

    #region ToNative
    //TO DO: implement conversion code for ToNative

    private List<GsaRecord> AxisToNative(Base @object)
    {
      var axis = (Axis)@object;

      var index = Instance.GsaModel.Cache.ResolveIndex<GsaAxis>(axis.applicationId);

      return new List<GsaRecord>
      {
        new GsaAxis()
        {
          ApplicationId = axis.applicationId,
          Name = axis.name,
          Index = index,
          OriginX = axis.definition.origin.x,
          OriginY = axis.definition.origin.y,
          OriginZ = axis.definition.origin.z
        }
      };
    }

    #endregion

    #region Helper
    #region ToSpeckle
    #region Geometry
    #region Node
    /// <summary>
    /// Conversion of node restraint from GSA to Speckle
    /// </summary>
    /// <param name="gsaNode">GsaNode object with the restraint definition to be converted</param>
    /// <returns></returns>
    private static Restraint GetRestraint(GsaNode gsaNode)
    {
      Restraint restraint;
      switch (gsaNode.NodeRestraint)
      {
        case NodeRestraint.Pin:
          restraint = new Restraint(RestraintType.Pinned);
          break;
        case NodeRestraint.Fix:
          restraint = new Restraint(RestraintType.Fixed);
          break;
        case NodeRestraint.Free:
          restraint = new Restraint(RestraintType.Free);
          break;
        case NodeRestraint.Custom:
          string code = GetCustomRestraintCode(gsaNode);
          restraint = new Restraint(code.ToString());
          break;
        default:
          restraint = new Restraint();
          break;
      }

      //restraint = UpdateSpringStiffness(restraint, gsaNode);

      return restraint;
    }

    /// <summary>
    /// Conversion of 1D element end releases from GSA to Speckle restraint
    /// </summary>
    /// <param name="release">Dictionary of release codes</param>
    /// <returns></returns>
    private static Restraint GetRestraint(Dictionary<AxisDirection6, ReleaseCode> release)
    {
      var code = new List<string>() { "F", "F", "F", "F", "F", "F" }; //Default
      if (release != null)
      {
        foreach (var k in release.Keys.ToList())
        {
          switch (k)
          {
            case AxisDirection6.X:
              code[0] = release[k].GetStringValue();
              break;
            case AxisDirection6.Y:
              code[1] = release[k].GetStringValue();
              break;
            case AxisDirection6.Z:
              code[2] = release[k].GetStringValue();
              break;
            case AxisDirection6.XX:
              code[3] = release[k].GetStringValue();
              break;
            case AxisDirection6.YY:
              code[4] = release[k].GetStringValue();
              break;
            case AxisDirection6.ZZ:
              code[5] = release[k].GetStringValue();
              break;
          }
        }
      }

      return new Restraint(string.Join("", code));
    }

    /// <summary>
    /// Conversion of node constraint axis from GSA to Speckle
    /// </summary>
    /// <param name="gsaNode">GsaNode object with the constraint axis definition to be converted</param>
    /// <returns></returns>
    private Plane GetConstraintAxis(GsaNode gsaNode)
    {
      Plane speckleAxis;
      Point origin;
      Vector xdir, ydir, normal;

      if (gsaNode.AxisRefType == NodeAxisRefType.XElevation)
      {
        origin = new Point(0, 0, 0);
        xdir = new Vector(0, -1, 0);
        ydir = new Vector(0, 0, 1);
        normal = new Vector(-1, 0, 0);
        speckleAxis = new Plane(origin, normal, xdir, ydir);
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.YElevation)
      {
        origin = new Point(0, 0, 0);
        xdir = new Vector(1, 0, 0);
        ydir = new Vector(0, 0, 1);
        normal = new Vector(0, -1, 0);
        speckleAxis = new Plane(origin, normal, xdir, ydir);
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.Vertical)
      {
        origin = new Point(0, 0, 0);
        xdir = new Vector(0, 0, 1);
        ydir = new Vector(1, 0, 0);
        normal = new Vector(0, 1, 0);
        speckleAxis = new Plane(origin, normal, xdir, ydir);
      }
      else if (gsaNode.AxisRefType == NodeAxisRefType.Reference && IsIndex(gsaNode.AxisIndex))
      {
        speckleAxis = GetAxisFromIndex(gsaNode.AxisIndex.Value).definition;
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        speckleAxis = GlobalAxis().definition;
      }

      return speckleAxis;
    }

    /// <summary>
    /// Speckle structural schema restraint code
    /// </summary>
    /// <param name="gsaNode">GsaNode object with the restraint definition to be converted</param>
    /// <returns></returns>
    private static string GetCustomRestraintCode(GsaNode gsaNode)
    {
      var code = "RRRRRR".ToCharArray();
      for (var i = 0; i < gsaNode.Restraints.Count(); i++)
      {
        switch (gsaNode.Restraints[i])
        {
          case AxisDirection6.X:
            code[0] = 'F';
            break;
          case AxisDirection6.Y:
            code[1] = 'F';
            break;
          case AxisDirection6.Z:
            code[2] = 'F';
            break;
          case AxisDirection6.XX:
            code[3] = 'F';
            break;
          case AxisDirection6.YY:
            code[4] = 'F';
            break;
          case AxisDirection6.ZZ:
            code[5] = 'F';
            break;
        }
      }
      return code.ToString();
    }

    /// <summary>
    /// Add GSA spring stiffness definition to Speckle restraint definition.
    /// Deprecated: Using GSANode instead of Node, so spring stiffness no longer stored in Restraint
    /// </summary>
    /// <param name="restraint">Restraint speckle object to be updated</param>
    /// <param name="gsaNode">GsaNode object with spring stiffness definition</param>
    /// <returns></returns>
    private Restraint UpdateSpringStiffness(Restraint restraint, GsaNode gsaNode)
    {
      //Spring Stiffness
      if (IsIndex(gsaNode.SpringPropertyIndex))
      {
        var gsaRecord = Instance.GsaModel.GetNative<GsaPropSpr>(gsaNode.SpringPropertyIndex.Value);
        if (gsaRecord.GetType() != typeof(GsaPropSpr))
        {
          return restraint;
        }
        var gsaSpring = (GsaPropSpr)gsaRecord;

        //Update spring stiffness
        if (gsaSpring.Stiffnesses[AxisDirection6.X] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[0] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessX = gsaSpring.Stiffnesses[AxisDirection6.X];
        }
        if (gsaSpring.Stiffnesses[AxisDirection6.Y] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[1] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessY = gsaSpring.Stiffnesses[AxisDirection6.Y];
        }
        if (gsaSpring.Stiffnesses[AxisDirection6.Z] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[2] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessZ = gsaSpring.Stiffnesses[AxisDirection6.Z];
        }
        if (gsaSpring.Stiffnesses[AxisDirection6.XX] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[3] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessXX = gsaSpring.Stiffnesses[AxisDirection6.XX];
        }
        if (gsaSpring.Stiffnesses[AxisDirection6.YY] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[4] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessYY = gsaSpring.Stiffnesses[AxisDirection6.YY];
        }
        if (gsaSpring.Stiffnesses[AxisDirection6.ZZ] > 0)
        {
          var code = restraint.code.ToCharArray();
          code[5] = 'K';
          restraint.code = code.ToString();
          restraint.stiffnessZZ = gsaSpring.Stiffnesses[AxisDirection6.ZZ];
        }
      }
      return restraint;
    }

    /// <summary>
    /// Get Speckle node object from GSA node index
    /// </summary>
    /// <param name="index">GSA node index</param>
    /// <returns></returns>
    private Node GetNodeFromIndex(int index)
    {
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaNode, Node>(index, out var speckleObjects) && speckleObjects != null && speckleObjects.Count > 0) 
        ? speckleObjects.First() : null;
      /*
      Node speckleNode = null;
      var gsaNode = Instance.GsaModel.GetNative<GsaNode>(index);
      if (gsaNode != null) speckleNode = GsaNodeToSpeckle((GsaNode)gsaNode);

      return speckleNode;
      */
    }
    #endregion

    #region Axis
    /// <summary>
    /// Get Speckle axis object from GSA axis index
    /// </summary>
    /// <param name="index">GSA axis index</param>
    /// <returns></returns>
    private Axis GetAxisFromIndex(int index)
    {
      var gsaAxis = Instance.GsaModel.GetNative<GsaAxis>(index);
      if (gsaAxis.GetType() != typeof(GsaAxis))
      {
        return null;
      }
      return GsaAxisToSpeckle((GsaAxis)gsaAxis);
    }

    /// <summary>
    /// Speckle global axis definition
    /// </summary>
    /// <returns></returns>
    private static Axis GlobalAxis()
    {
      //Default global coordinates for case: Global or NotSet
      var origin = new Point(0, 0, 0);
      var xdir = new Vector(1, 0, 0);
      var ydir = new Vector(0, 1, 0);
      var normal = new Vector(0, 0, 1);

      var axis = new Axis()
      {
        name = "",
        axisType = AxisType.Cartesian,
        definition = new Plane(origin, normal, xdir, ydir)
      };

      return axis;
    }
    #endregion

    #region Elements
    /// <summary>
    /// Determine if object represents a 2D element
    /// </summary>
    /// <param name="gsaEl">GsaEl object containing the element definition</param>
    /// <returns></returns>
    public bool Is2dElement(GsaEl gsaEl)
    {
      return (gsaEl.Type == ElementType.Triangle3 || gsaEl.Type == ElementType.Triangle6 || gsaEl.Type == ElementType.Quad4 || gsaEl.Type == ElementType.Quad8);
    }

    /// <summary>
    /// Determine if object represents a 3D element
    /// </summary>
    /// <param name="gsaEl">GsaEl object containing the element definition</param>
    /// <returns></returns>
    public bool Is3dElement(GsaEl gsaEl)
    {
      return (gsaEl.Type == ElementType.Brick8 || gsaEl.Type == ElementType.Pyramid5 || gsaEl.Type == ElementType.Tetra4 || gsaEl.Type == ElementType.Wedge6);
    }

    /// <summary>
    /// Determine if object represents a 1D element
    /// </summary>
    /// <param name="gsaEl">GsaEl object containing the element definition</param>
    /// <returns></returns>
    public bool Is1dElement(GsaEl gsaEl)
    {
      return (gsaEl.Type == ElementType.Bar || gsaEl.Type == ElementType.Beam || gsaEl.Type == ElementType.Cable || gsaEl.Type == ElementType.Damper || 
        gsaEl.Type == ElementType.Link || gsaEl.Type == ElementType.Rod || gsaEl.Type == ElementType.Spacer || gsaEl.Type == ElementType.Spring || 
        gsaEl.Type == ElementType.Strut || gsaEl.Type == ElementType.Tie);
    }

    private ElementType1D GetElement1dType(ElementType gsaType)
    {
      ElementType1D speckleType;

      switch (gsaType)
      {
        case ElementType.Bar:
          speckleType = ElementType1D.Bar;
          break;
        case ElementType.Cable:
          speckleType = ElementType1D.Cable;
          break;
        case ElementType.Damper:
          speckleType = ElementType1D.Damper;
          break;
        case ElementType.Link:
          speckleType = ElementType1D.Link;
          break;
        case ElementType.Rod:
          speckleType = ElementType1D.Rod;
          break;
        case ElementType.Spacer:
          speckleType = ElementType1D.Spacer;
          break;
        case ElementType.Spring:
          speckleType = ElementType1D.Spring;
          break;
        case ElementType.Strut:
          speckleType = ElementType1D.Strut;
          break;
        case ElementType.Tie:
          speckleType = ElementType1D.Tie;
          break;
        default:
          speckleType = ElementType1D.Beam;
          break;
      }

      return speckleType;
    }

    /// <summary>
    /// Get the local axis for a 1D element
    /// </summary>
    /// <param name="n1">end1Node</param>
    /// <param name="n2">end2Node</param>
    /// <param name="n3">orientationNode</param>
    /// <param name="angle">orientationAngle in radians</param>
    /// <returns></returns>
    private Plane GetLocalAxis(Node n1, Node n2, Node n3, double angle)
    {
      var normal = new Vector(0, 0, 1); //default

      var p1 = n1.basePoint;
      var p2 = n2.basePoint;
      var origin = new Point(p1.x, p1.y, p1.z);
      var xdir = Vector.UnitVector(new Vector(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z));

      //Update normal if orientation node exists
      if (n3 != null)
      {
        var p3 = n3.basePoint;
        normal = Vector.UnitVector(new Vector(p3.x - p1.x, p3.y - p1.y, p3.z - p1.z));
      }

      //Apply rotation angle
      if (angle != 0) normal = Vector.UnitVector(Rotate(normal, xdir, angle));

      //xdir and normal define a plane:
      // *ensure normal is perpendicular to xdir on that plane
      // *ensure ydir is normal to the plane
      var ydir = -Vector.UnitVector(xdir * normal);
      normal = Vector.UnitVector(xdir * ydir);

      return new Plane(origin, normal, xdir, ydir);
    }
    #endregion
    #endregion

    #region Loading
    #endregion

    #region Materials
    //Some material properties are stored in either GsaMat or GsaMatAnal

    /// <summary>
    /// Return true if either v1 or v2 has a value.
    /// </summary>
    /// <param name="v1">value to take precidence if not null</param>
    /// <param name="v2">value to take if v1 is null</param>
    /// <param name="v">returned value</param>
    /// <returns></returns>
    public bool Choose(double? v1, double? v2, out double v)
    {
      if (v1.HasValue)
      {
        v = v1.Value;
        return true;
      }
      else if (v2.HasValue)
      {
        v = v2.Value;
        return true;
      }
      else
      {
        v = 0;
        return false;
      }
    }

    /// <summary>
    /// Get Speckle material object from GSA material index
    /// </summary>
    /// <param name="index">GSA material index</param>
    /// <param name="type">GSA material type</param>
    /// <returns></returns>
    private Material GetMaterialFromIndex(int index, Property2dMaterialType type)
    {
      //Initialise
      GsaRecord gsaMat;
      Material speckleMaterial = null;   

      //Get material based on type and gsa index
      //Convert gsa material to speckle material
      if (type == Property2dMaterialType.Steel)
      {
        gsaMat = Instance.GsaModel.GetNative<GsaMatSteel>(index);
        if (gsaMat != null) speckleMaterial = GsaMaterialSteelToSpeckle((GsaMatSteel)gsaMat);
      }
      else if (type == Property2dMaterialType.Concrete)
      {
        gsaMat = Instance.GsaModel.GetNative<GsaMatConcrete>(index);
        if (gsaMat != null) speckleMaterial = GsaMaterialConcreteToSpeckle((GsaMatConcrete)gsaMat);
      }

      return speckleMaterial;
    }

    /// <summary>
    /// Get Speckle material object from GSA material index
    /// </summary>
    /// <param name="index">GSA material index</param>
    /// <param name="type">GSA material type</param>
    /// <returns></returns>
    private Material GetMaterialFromIndex(int index, Section1dMaterialType type)
    {
      //Initialise
      GsaRecord gsaMat;
      Material speckleMaterial = null;

      //Get material based on type and gsa index
      //Convert gsa material to speckle material
      if (type == Section1dMaterialType.STEEL)
      {
        gsaMat = Instance.GsaModel.GetNative<GsaMatSteel>(index);
        if (gsaMat != null) speckleMaterial = GsaMaterialSteelToSpeckle((GsaMatSteel)gsaMat);
      }
      else if (type == Section1dMaterialType.CONCRETE)
      {
        gsaMat = Instance.GsaModel.GetNative<GsaMatConcrete>(index);
        if (gsaMat != null) speckleMaterial = GsaMaterialConcreteToSpeckle((GsaMatConcrete)gsaMat);
      }

      return speckleMaterial;
    }
    #endregion

    #region Properties
    #region Spring
    /// <summary>
    /// Get Speckle PropertySpring object from GSA property spring index
    /// </summary>
    /// <param name="index">GSA property spring index</param>
    /// <returns></returns>
    private PropertySpring GetPropertySpringFromIndex(int index)
    {
      PropertySpring specklePropertySpring = null;
      var gsaPropSpr = Instance.GsaModel.GetNative<GsaPropSpr>(index);
      if (gsaPropSpr != null) specklePropertySpring = GsaPropertySpringToSpeckle((GsaPropSpr)gsaPropSpr);

      return specklePropertySpring;
    }

    /// <summary>
    /// Set properties for an axial spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringAxial(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Axial;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a torsional spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetPropertySpringTorsional(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Torsional;
      specklePropertySpring.stiffnessXX = gsaPropSpr.Stiffnesses[AxisDirection6.XX];
      return true;
    }

    /// <summary>
    /// Set properties for a compression only spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringCompression(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.CompressionOnly;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a tension only spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringTension(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.TensionOnly;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a lockup spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringLockup(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      //Also for LOCKUP, there are positive and negative parameters, but these aren't supported yet
      specklePropertySpring.springType = PropertyTypeSpring.LockUp;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      specklePropertySpring.positiveLockup = 0;
      specklePropertySpring.negativeLockup = 0;
      return true;
    }

    /// <summary>
    /// Set properties for a gap spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringGap(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Gap;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      return true;
    }

    /// <summary>
    /// Set properties for a friction spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringFriction(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.Friction;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      specklePropertySpring.stiffnessY = gsaPropSpr.Stiffnesses[AxisDirection6.Y];
      specklePropertySpring.stiffnessZ = gsaPropSpr.Stiffnesses[AxisDirection6.Z];
      specklePropertySpring.frictionCoefficient = gsaPropSpr.FrictionCoeff.Value;
      return true;
    }

    /// <summary>
    /// Set properties for a general spring
    /// </summary>
    /// <param name="gsaPropSpr">GsaPropSpr object containing the spring definition</param>
    /// <param name="specklePropertySpring">Speckle PropertySPring object to be updated</param>
    /// <returns></returns>
    private bool SetProprtySpringGeneral(GsaPropSpr gsaPropSpr, PropertySpring specklePropertySpring)
    {
      specklePropertySpring.springType = PropertyTypeSpring.General;
      specklePropertySpring.stiffnessX = gsaPropSpr.Stiffnesses[AxisDirection6.X];
      specklePropertySpring.springCurveX = 0;
      specklePropertySpring.stiffnessY = gsaPropSpr.Stiffnesses[AxisDirection6.Y];
      specklePropertySpring.springCurveY = 0;
      specklePropertySpring.stiffnessZ = gsaPropSpr.Stiffnesses[AxisDirection6.Z];
      specklePropertySpring.springCurveZ = 0;
      specklePropertySpring.stiffnessXX = gsaPropSpr.Stiffnesses[AxisDirection6.XX];
      specklePropertySpring.springCurveXX = 0;
      specklePropertySpring.stiffnessYY = gsaPropSpr.Stiffnesses[AxisDirection6.YY];
      specklePropertySpring.springCurveYY = 0;
      specklePropertySpring.stiffnessZZ = gsaPropSpr.Stiffnesses[AxisDirection6.ZZ];
      specklePropertySpring.springCurveZZ = 0;
      return true;
    }
    #endregion

    #region Mass
    /// <summary>
    /// Get Speckle PropertyMass object from GSA property mass index
    /// </summary>
    /// <param name="index">GSA property mass index</param>
    /// <returns></returns>
    private PropertyMass GetPropertyMassFromIndex(int index)
    {
      PropertyMass specklePropertyMass = null;
      var gsaPropMass = Instance.GsaModel.GetNative<GsaPropMass>(index);
      if (gsaPropMass != null) specklePropertyMass = GsaPropertyMassToSpeckle((GsaPropMass)gsaPropMass);

      return specklePropertyMass;
    }
    #endregion

    #region Property1D
    private BaseReferencePoint GetReferencePoint(ReferencePoint gsaReferencePoint)
    {
      switch(gsaReferencePoint)
      {
        case ReferencePoint.BottomCentre:
          return BaseReferencePoint.BotCentre;
        case ReferencePoint.BottomLeft:
          return BaseReferencePoint.BotLeft;
        default:
          return BaseReferencePoint.Centroid;
      }
    }

    private SectionProfile GetProfile(ProfileDetails gsaProfile)
    {
      var speckleProfile = new SectionProfile()
      {
        shapeDescription = gsaProfile.ToDesc()
      };

      return speckleProfile;
    }

    /// <summary>
    /// Get Speckle Property1D object from GSA property 1D index
    /// </summary>
    /// <param name="index">GSA property 1D index</param>
    /// <returns></returns>
    private Property1D GetProperty1dFromIndex(int index)
    {
      Property1D speckleProperty1d = null;
      var gsaSection = Instance.GsaModel.GetNative<GsaSection>(index);
      if (gsaSection != null) speckleProperty1d = GsaSectionToSpeckle((GsaSection)gsaSection);

      return speckleProperty1d;
    }
    #endregion

    #region Property2D
    /// <summary>
    /// Converts the GsaProp2d reference surface to Speckle
    /// </summary>
    /// <param name="gsaProp2d">GsaProp2d object with reference surface definition</param>
    /// <returns></returns>
    private ReferenceSurface GetReferenceSurface(GsaProp2d gsaProp2d)
    {
      var refenceSurface = ReferenceSurface.Middle; //default

      if (gsaProp2d.RefPt == Property2dRefSurface.BottomCentre)
      {
        refenceSurface = ReferenceSurface.Bottom;
      }
      else if (gsaProp2d.RefPt == Property2dRefSurface.TopCentre)
      {
        refenceSurface = ReferenceSurface.Top;
      }
      return refenceSurface;
    }

    /// <summary>
    /// Convert GSA 2D element reference axis to Speckle
    /// </summary>
    /// <param name="gsaProp2d">GsaProp2d object with reference axis definition</param>
    /// <returns></returns>
    private Axis GetOrientationAxis(GsaProp2d gsaProp2d)
    {
      //Cartesian coordinate system is the only one supported.
      var orientationAxis = new Axis()
      {
        name = "",
        axisType = AxisType.Cartesian,
      };

      if (gsaProp2d.AxisRefType == AxisRefType.Local)
      {
        //TO DO: handle local reference axis case
        //Local would be a different coordinate system for each element that gsaProp2d was assigned to
      }
      else if (gsaProp2d.AxisRefType == AxisRefType.Reference && IsIndex(gsaProp2d.AxisIndex))
      {
        orientationAxis = GetAxisFromIndex(gsaProp2d.AxisIndex.Value);
      }
      else
      {
        //Default global coordinates for case: Global or NotSet
        orientationAxis = GlobalAxis();
      }

      return orientationAxis;
    }

    /// <summary>
    /// Get Speckle Property2D object from GSA property 2D index
    /// </summary>
    /// <param name="index">GSA property 2D index</param>
    /// <returns></returns>
    private Property2D GetProperty2dFromIndex(int index)
    {
      /*
      Property2D speckleProperty2d = null;
      var gsaProp2d = Instance.GsaModel.GetNative<GsaProp2d>(index);
      if (gsaProp2d != null) speckleProperty2d = GsaProperty2dToSpeckle((GsaProp2d)gsaProp2d);
      */
      return (Instance.GsaModel.Cache.GetSpeckleObjects<GsaProp2d, Property2D>(index, out var speckleObjects)) ? speckleObjects.First() : null;
    }
    #endregion
    #endregion
    #endregion

    #region ToNative
    #endregion

    #region Other
    /// <summary>
    /// Test if a nullable integer has a value and is positive
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private bool IsIndex(int? index)
    {
      return (index.HasValue && index.Value > 0);
    }

    /// <summary>
    /// Test if a nullable double has a value and is positive
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private bool IsPositive(double? value)
    {
      return (value.HasValue && value.Value > 0);
    }

    /// <summary>
    /// Convert angle from degrees to radians
    /// </summary>
    /// <param name="degrees">angle in degrees</param>
    /// <returns></returns>
    private double Radians(double degrees)
    {
      return Math.PI * degrees / 180;
    }
    #region Vector

    /// <summary>
    /// Rotate vector V by an angle Theta about unit vector K using right hand rule
    /// </summary>
    /// <param name="v">vector to be rotated</param>
    /// <param name="k">unit vector defining axis of rotation</param>
    /// <param name="theta">rotation angle (radians)</param>
    /// <returns></returns>
    private Vector Rotate(Vector v, Vector k, double theta)
    {
      //Rodrigues' rotation formula
      //https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula

      k = Vector.UnitVector(k); //ensure axis of rotation is a unit vector
      var v_rot1 = v * Math.Cos(theta);
      var v_rot2 = (k * v) * Math.Sin(theta);
      var v_rot3 = k * (Vector.DotProduct(k, v) * (1 - Math.Sin(theta)));

      return v_rot1 + v_rot2 + v_rot3;
    }
    #endregion
    #endregion
    #endregion
  }
}
