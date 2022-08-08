﻿using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Acad = Autodesk.AutoCAD;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using BlockDefinition = Objects.Other.BlockDefinition;
using BlockInstance = Objects.Other.BlockInstance;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Dimension = Objects.Other.Dimension;
using Ellipse = Objects.Geometry.Ellipse;
using Hatch = Objects.Other.Hatch;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using ModelCurve = Objects.BuiltElements.Revit.Curve.ModelCurve;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Spiral = Objects.Geometry.Spiral;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
#if CIVIL2021 || CIVIL2022 || CIVIL2023
using Civil = Autodesk.Civil;
using CivilDB = Autodesk.Civil.DatabaseServices;
#endif


namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil : ISpeckleConverter
  {
#if AUTOCAD2021
    public static string AutocadAppName = VersionedHostApplications.Autocad2021;
#elif AUTOCAD2022
    public static string AutocadAppName = VersionedHostApplications.Autocad2022;
#elif AUTOCAD2023
    public static string AutocadAppName = VersionedHostApplications.Autocad2023;
#elif CIVIL2021
    public static string AutocadAppName = VersionedHostApplications.Civil2021;
#elif CIVIL2022
    public static string AutocadAppName = VersionedHostApplications.Civil2022;
#elif CIVIL2023
    public static string AutocadAppName = VersionedHostApplications.Civil2023;
#endif

    public ConverterAutocadCivil()
    {
      var ver = System.Reflection.Assembly.GetAssembly(typeof(ConverterAutocadCivil)).GetName().Version;
    }

    #region ISpeckleConverter props
    public string Description => "Default Speckle Kit for AutoCAD";
    public string Name => nameof(ConverterAutocadCivil);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";
    public ProgressReport Report { get; private set; } = new ProgressReport();
    public IEnumerable<string> GetServicedApplications() => new string[] { AutocadAppName };
    public Document Doc { get; private set; }
    public Transaction Trans { get; private set; } // TODO: evaluate if this should be here
    #endregion ISpeckleConverter props

    public ReceiveMode ReceiveMode { get; set; }

    public List<ApplicationObject> ContextObjects { get; set; } = new List<ApplicationObject>();

    public void SetContextObjects(List<ApplicationObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationObject> objects) => throw new NotImplementedException();
    public void SetConverterSettings(object settings)
    {
      throw new NotImplementedException("This converter does not have any settings.");
    }

    public void SetContextDocument(object doc)
    {
      Doc = (Document)doc;
      Trans = Doc.TransactionManager.TopTransaction; // set the stream transaction here! make sure it is the top level transaction
    }

    public Base ConvertToSpeckle(object @object)
    {
      Base @base = null;
      ApplicationObject reportObj = null;
      List<string> notes = new List<string>();

      switch (@object)
      {
        case DBObject obj:
          /*
          // check for speckle schema xdata
          string schema = GetSpeckleSchema(o.XData);
          if (schema != null)
            return ObjectToSpeckleBuiltElement(o);
          */
          reportObj = new ApplicationObject(obj.Id.ToString(), obj.GetType().ToString());
          switch (obj)
          {
            case DBPoint o:
              @base = PointToSpeckle(o);
              break;
            case AcadDB.Line o:
              @base = LineToSpeckle(o);
              break;
            case AcadDB.Arc o:
              @base = ArcToSpeckle(o);
              break;
            case AcadDB.Circle o:
              @base = CircleToSpeckle(o);
              break;
            case AcadDB.Ellipse o:
              @base = EllipseToSpeckle(o);
              break;
            case AcadDB.Hatch o:
              @base = HatchToSpeckle(o);
              break;
            case AcadDB.Spline o:
              @base = SplineToSpeckle(o);
              break;
            case AcadDB.Polyline o:
              if (o.IsOnlyLines) // db polylines can have arc segments, decide between polycurve or polyline conversion
                @base = PolylineToSpeckle(o);
              else
                @base = PolycurveToSpeckle(o);
              break;
            case AcadDB.Polyline3d o:
              @base = PolylineToSpeckle(o);
              break;
            case Polyline2d o:
              @base = PolycurveToSpeckle(o);
              break;
            case Region o:
              @base = RegionToSpeckle(o, out notes);
              break;
            case AcadDB.Surface o:
              @base = SurfaceToSpeckle(o, out notes);
              break;
            case PolyFaceMesh o:
              @base = MeshToSpeckle(o);
              break;
            case SubDMesh o:
              @base = MeshToSpeckle(o);
              break;
            case Solid3d o:
              if (o.IsNull)
                notes.Add($"Solid was null");
              else
                @base = SolidToSpeckle(o, out notes);
              break;
            case AcadDB.Dimension o:
              @base = DimensionToSpeckle(o);
              break;
            case BlockReference o:
              @base = BlockReferenceToSpeckle(o);
              break;
            case BlockTableRecord o:
              @base = BlockRecordToSpeckle(o);
              break;
            case DBText o:
              @base = TextToSpeckle(o);
              break;
            case MText o:
              @base = TextToSpeckle(o);
              break;
#if CIVIL2021 || CIVIL2022 || CIVIL2023
            case CivilDB.Alignment o:
              @base = AlignmentToSpeckle(o);
              break;
            case CivilDB.Corridor o:
              @base = CorridorToSpeckle(o);
              break;
            case CivilDB.FeatureLine o:
              @base = FeatureLineToSpeckle(o);
              break;
            case CivilDB.Structure o:
              @base = StructureToSpeckle(o);
              break;
            case CivilDB.Pipe o:
              @base = PipeToSpeckle(o);
              break;
            case CivilDB.PressurePipe o:
              @base = PipeToSpeckle(o);
              break;
            case CivilDB.Profile o:
              @base = ProfileToSpeckle(o);
              break;
            case CivilDB.TinSurface o:
              @base = SurfaceToSpeckle(o);
              break;
#endif
          }

          DisplayStyle style = DisplayStyleToSpeckle(obj as Entity);
          if (style != null)
            @base["displayStyle"] = style;
          break;

        case Acad.Geometry.Point3d o:
          @base = PointToSpeckle(o);
          break;

        case Acad.Geometry.Vector3d o:
          @base = VectorToSpeckle(o);
          break;

        case Acad.Geometry.Line3d o:
          @base = LineToSpeckle(o);
          break;

        case Acad.Geometry.LineSegment3d o:
          @base = LineToSpeckle(o);
          break;

        case Acad.Geometry.CircularArc3d o:
          @base = ArcToSpeckle(o);
          break;

        case Acad.Geometry.Plane o:
          @base = PlaneToSpeckle(o);
          break;

        case Acad.Geometry.Curve3d o:
          @base = CurveToSpeckle(o) as Base;
          break;

        default:
          throw new NotSupportedException();
      }
      if (reportObj != null)
      {
        reportObj.Update(log: notes);
        Report.UpdateReportObject(reportObj);
      }
      return @base;
    }

    private Base ObjectToSpeckleBuiltElement(DBObject o)
    {
      throw new NotImplementedException();
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public object ConvertToNative(Base @object)
    {
      // determine if this object has autocad props
      bool isFromAutoCAD = @object[AutocadPropName] != null ? true : false; 
      object acadObj = null;
      var reportObj = Report.GetReportObject(@object.id, out int index) ? new ApplicationObject(@object.id, @object.speckle_type) : null;
      List<string> notes = new List<string>();
      switch (@object)
      {
        case Point o:
          acadObj = PointToNativeDB(o);
          break;

        case Line o:
          acadObj = LineToNativeDB(o);
          break;

        case Arc o:
          acadObj = ArcToNativeDB(o);
          break;

        case Circle o:
          acadObj = CircleToNativeDB(o);
          break;

        case Ellipse o:
          acadObj = EllipseToNativeDB(o);
          break;

        case Spiral o:
          acadObj = PolylineToNativeDB(o.displayValue);
          break;

        case Hatch o:
          acadObj = HatchToNativeDB(o);
          break;

        case Polyline o:
          acadObj = PolylineToNativeDB(o);
          break;

        case Polycurve o:
          bool convertAsSpline = (o.segments.Where(s => !(s is Line) && !(s is Arc)).Count() > 0) ? true : false;
          if (!convertAsSpline) convertAsSpline = IsPolycurvePlanar(o) ? false : true;
          acadObj = convertAsSpline ? PolycurveSplineToNativeDB(o) : PolycurveToNativeDB(o);
          break;

        case Curve o:
          acadObj = CurveToNativeDB(o);
          break;

        /*
        case Surface o: 
          return SurfaceToNative(o);

        case Brep o:
          acadObj = (o.displayMesh != null) ? MeshToNativeDB(o.displayMesh) : null;
          break;
        */

        case Mesh o:
          acadObj = MeshToNativeDB(o);
          break;

        case Dimension o:
          acadObj = isFromAutoCAD ? AcadDimensionToNative(o) : DimensionToNative(o);
          break;

        case BlockInstance o:
          acadObj = BlockInstanceToNativeDB(o, out BlockReference reference);
          break;

        case BlockDefinition o:
          acadObj = BlockDefinitionToNativeDB(o);
          break;

        case Text o:
          acadObj = isFromAutoCAD ? AcadTextToNative(o) : TextToNative(o);
          Report.Log($"Created Text {o.id}");
          break;

        case Alignment o:
#if CIVIL2021 || CIVIL2022 || CIVIL2023
          acadObj = AlignmentToNative(o);
#endif
          acadObj = PolylineToNativeDB(o.displayValue);
          break;

        case ModelCurve o:
          acadObj = CurveToNativeDB(o.baseCurve);
          break;

        default:
          if (reportObj != null)
          {
            reportObj.Update(status: ApplicationObject.State.Skipped, logItem: $"{@object.GetType()} type not supported");
            Report.UpdateReportObject(reportObj);
          }
          throw new NotSupportedException();
      }

      if (reportObj != null)
      {
        reportObj.Update(log: notes);
        Report.UpdateReportObject(reportObj);
      }

      return acadObj;
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList();
    }

    public bool CanConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DBObject o:
          switch (o)
          {
            case DBPoint _:
            case AcadDB.Line _:
            case AcadDB.Arc _:
            case AcadDB.Circle _:
            case AcadDB.Dimension _:
            case AcadDB.Ellipse _:
            case AcadDB.Hatch _:
            case AcadDB.Spline _:
            case AcadDB.Polyline _:
            case AcadDB.Polyline2d _:
            case AcadDB.Polyline3d _:
            case AcadDB.Surface _:
            case AcadDB.PolyFaceMesh _:
            case AcadDB.Region _:
            case SubDMesh _:
            case Solid3d _:
              return true;

            case BlockReference _:
            case BlockTableRecord _:
            case AcadDB.DBText _:
            case AcadDB.MText _:
              return true;

#if CIVIL2021 || CIVIL2022 || CIVIL2023
            // NOTE: C3D pressure pipes and pressure fittings API under development
            case CivilDB.FeatureLine _:
            case CivilDB.Corridor _:
            case CivilDB.Structure _:
            case CivilDB.Alignment _:
            case CivilDB.Pipe _:
            case CivilDB.PressurePipe _:
            case CivilDB.Profile _:
            case CivilDB.TinSurface _:
              return true;
#endif

            default:
              return false;
          }

        case Acad.Geometry.Point3d _:
        case Acad.Geometry.Vector3d _:
        case Acad.Geometry.Plane _:
        case Acad.Geometry.Line3d _:
        case Acad.Geometry.LineSegment3d _:
        case Acad.Geometry.CircularArc3d _:
        case Acad.Geometry.Curve3d _:
          return true;

        default:
          return false;
      }
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point _:
        case Line _:
        case Arc _:
        case Circle _:
        case Ellipse _:
        case Spiral _:
        case Hatch _:
        case Polyline _:
        case Polycurve _:
        case Curve _:
        //case Brep _:
        case Mesh _:

        case Dimension _:
        case BlockDefinition _:
        case BlockInstance _:
        case Text _:

        case Alignment _:

        case ModelCurve _:
          return true;

        default:
          return false;
      }
    }

  }
}
