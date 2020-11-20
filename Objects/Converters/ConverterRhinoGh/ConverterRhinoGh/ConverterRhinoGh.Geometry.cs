﻿using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Rhino.Geometry;
using Speckle.Core.Models;
using Objects;
using Objects.Geometry;
using Objects.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using Brep = Objects.Geometry.Brep;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Extrusion = Objects.Geometry.Extrusion;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;
using RH = Rhino.Geometry;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.RhinoGh
{
  public static partial class Conversion
  {
    // Convenience methods point:
    public static double[] ToArray(this Point3d pt)
    {
      return new double[] { pt.X, pt.Y, pt.Z };
    }

    public static double[] ToArray(this Point2d pt)
    {
      return new double[] { pt.X, pt.Y };
    }

    public static double[] ToArray(this Point2f pt)
    {
      return new double[] { pt.X, pt.Y };
    }

    public static Point3d ToPoint(this double[] arr)
    {
      return new Point3d(arr[0], arr[1], arr[2]);
    }


    // Mass point converter
    public static Point3d[] ToPoints(this IEnumerable<double> arr)
    {
      if (arr.Count() % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

      Point3d[] points = new Point3d[arr.Count() / 3];
      var asArray = arr.ToArray();
      for (int i = 2, k = 0; i < arr.Count(); i += 3)
        points[k++] = new Point3d(asArray[i - 2], asArray[i - 1], asArray[i]);

      return points;
    }

    public static double[] ToFlatArray(this IEnumerable<Point3d> points)
    {
      return points.SelectMany(pt => pt.ToArray()).ToArray();
    }

    public static double[] ToFlatArray(this IEnumerable<Point2f> points)
    {
      return points.SelectMany(pt => pt.ToArray()).ToArray();
    }

    // Convenience methods vector:
    public static double[] ToArray(this Vector3d vc)
    {
      return new double[] { vc.X, vc.Y, vc.Z };
    }

    public static Vector3d ToVector(this double[] arr)
    {
      return new Vector3d(arr[0], arr[1], arr[2]);
    }

    // Points
    // GhCapture?
    public static Point ToSpeckle(this Point3d pt)
    {
      return new Point(pt.X, pt.Y, pt.Z);
    }
    // Rh Capture?
    public static Rhino.Geometry.Point ToNative(this Point pt)
    {
      var myPoint = new Rhino.Geometry.Point(new Point3d(pt.value[0], pt.value[1], pt.value[2]));

      return myPoint;
    }

    public static Point ToSpeckle(this Rhino.Geometry.Point pt)
    {
      return new Point(pt.Location.X, pt.Location.Y, pt.Location.Z);
    }

    // Vectors
    public static Vector ToSpeckle(this Vector3d pt)
    {
      return new Vector(pt.X, pt.Y, pt.Z);
    }

    public static Vector3d ToNative(this Vector pt)
    {
      return new Vector3d(pt.value[0], pt.value[1], pt.value[2]);
    }

    // Interval
    public static Interval ToSpeckle(this RH.Interval interval)
    {
      var speckleInterval = new Interval(interval.T0, interval.T1);
      return speckleInterval;
    }

    public static RH.Interval ToNative(this Interval interval)
    {
      return new RH.Interval((double)interval.start, (double)interval.end);
    }

    // Interval2d
    public static Interval2d ToSpeckle(this UVInterval interval)
    {
      return new Interval2d(interval.U.ToSpeckle(), interval.V.ToSpeckle());
    }

    public static UVInterval ToNative(this Interval2d interval)
    {
      return new UVInterval(interval.u.ToNative(), interval.v.ToNative());
    }

    // Plane
    public static Plane ToSpeckle(this RH.Plane plane)
    {
      return new Plane(plane.Origin.ToSpeckle(), plane.Normal.ToSpeckle(), plane.XAxis.ToSpeckle(), plane.YAxis.ToSpeckle());
    }

    public static RH.Plane ToNative(this Plane plane)
    {
      var xAxis = plane.xdir.ToNative();
      xAxis.Unitize();
      var yAxis = plane.ydir.ToNative();
      yAxis.Unitize();
      
      return new RH.Plane(plane.origin.ToNative().Location, xAxis,yAxis);
    }

    // Line
    // Gh Line capture
    public static Line ToSpeckle(this RH.Line line)
    {
      return new Line((new Point3d[] { line.From, line.To }).ToFlatArray());
    }

    // Rh Line capture
    public static Line ToSpeckle(this LineCurve line)
    {
      return new Line((new Point3d[] { line.PointAtStart, line.PointAtEnd }).ToFlatArray())
      {
        domain = line.Domain.ToSpeckle()
      };
    }

    // Back again only to LINECURVES because we hate grasshopper and its dealings with rhinocommon
    public static LineCurve ToNative(this Line line)
    {
      var pts = line.value.ToPoints();
      var myLine = new LineCurve(pts[0], pts[1]);
      if (line.domain != null)
        myLine.Domain = line.domain.ToNative();
      
      return myLine;
    }

    // Rectangles now and forever forward will become polylines
    public static Polyline ToSpeckle(this Rectangle3d rect)
    {
      return new Polyline((new Point3d[] { rect.Corner(0), rect.Corner(1), rect.Corner(2), rect.Corner(3) }).ToFlatArray()) { closed = true };
    }

    // Circle
    // Gh Capture
    public static Circle ToSpeckle(this RH.Circle circ)
    {
      var circle = new Circle(circ.Plane.ToSpeckle(), circ.Radius);
      return circle;
    }

    public static ArcCurve ToNative(this Circle circ)
    {
      RH.Circle circle = new RH.Circle(circ.plane.ToNative(), (double)circ.radius);

      var myCircle = new ArcCurve(circle);
      if (circ.domain != null)
        myCircle.Domain = circ.domain.ToNative();

      return myCircle;
    }

    // Arc
    // Rh Capture can be a circle OR an arc
    public static Base ToSpeckle(this ArcCurve a)
    {
      if (a.IsClosed)
      {
        RH.Circle preCircle;
        a.TryGetCircle(out preCircle);
        Circle myCircle = preCircle.ToSpeckle();
        myCircle.domain = a.Domain.ToSpeckle();
        
        return myCircle;
      }
      else
      {
        RH.Arc preArc;
        a.TryGetArc(out preArc);
        Arc myArc = preArc.ToSpeckle();
        myArc.domain = a.Domain.ToSpeckle();
        
        return myArc;
      }
    }

    // Gh Capture
    public static Arc ToSpeckle(this RH.Arc a)
    {
      Arc arc = new Arc(a.Plane.ToSpeckle(), a.Radius, a.StartAngle, a.EndAngle, a.Angle);
      arc.endPoint = a.EndPoint.ToSpeckle();
      arc.startPoint = a.StartPoint.ToSpeckle();
      arc.midPoint = a.MidPoint.ToSpeckle();
      return arc;
    }

    public static ArcCurve ToNative(this Arc a)
    {
      RH.Arc arc = new RH.Arc(a.plane.ToNative(), (double)a.radius, (double)a.angleRadians);
      arc.StartAngle = (double)a.startAngle;
      arc.EndAngle = (double)a.endAngle;
      var myArc = new ArcCurve(arc);

      if (a.domain != null)
      {
        myArc.Domain = a.domain.ToNative();
      }

      return myArc;
    }

    //Ellipse
    public static Ellipse ToSpeckle(this RH.Ellipse e)
    {
      return new Ellipse(e.Plane.ToSpeckle(), e.Radius1, e.Radius2);
    }

    public static RH.Curve ToNative(this Ellipse e)
    {
      RH.Plane plane = e.plane.ToNative();
      RH.Ellipse elp = new RH.Ellipse(plane, (double) e.firstRadius, (double) e.secondRadius); 
      
      var myEllp = elp.ToNurbsCurve();

      if (e.domain != null)
        myEllp.Domain = e.domain.ToNative();

      if (e.trimDomain != null)
        myEllp = myEllp.Trim(e.trimDomain.ToNative()).ToNurbsCurve();
      
      return myEllp;
    }

    // Polyline
    // Gh Capture
    public static Base ToSpeckle(this RH.Polyline poly)
    {
      if (poly.Count == 2)
        return new Line(poly.ToFlatArray());

      var myPoly = new Polyline(poly.ToFlatArray());
      myPoly.closed = poly.IsClosed;

      if (myPoly.closed)
        myPoly.value.RemoveRange(myPoly.value.Count - 3, 3);

      return myPoly;
    }

    // Rh Capture
    public static Base ToSpeckle(this PolylineCurve poly)
    {
      RH.Polyline polyline;

      if (poly.TryGetPolyline(out polyline))
      {
        if (polyline.Count == 2)
          return new Line(polyline.ToFlatArray(), null);

        var myPoly = new Polyline(polyline.ToFlatArray());
        myPoly.closed = polyline.IsClosed;

        if (myPoly.closed)
          myPoly.value.RemoveRange(myPoly.value.Count - 3, 3);

        myPoly.domain = poly.Domain.ToSpeckle();

        return myPoly;
      }
      return null;
    }

    // Deserialise
    public static PolylineCurve ToNative(this Polyline poly)
    {
      var points = poly.value.ToPoints().ToList();
      if (poly.closed) points.Add(points[0]);

      var myPoly = new PolylineCurve(points);
      if (poly.domain != null)
        myPoly.Domain = poly.domain.ToNative();

      return myPoly;
    }

    // Polycurve
    // Rh Capture/Gh Capture
    public static Polycurve ToSpeckle(this PolyCurve p)
    {
      Polycurve myPoly = new Polycurve();
      myPoly.closed = p.IsClosed;
      myPoly.domain = p.Domain.ToSpeckle();

      var segments = new List<RH.Curve>();
      CurveSegments(segments, p, true);

      //let the converter pick the best type of curve
      var c = new ConverterRhinoGh();
      myPoly.segments = segments.Select(s => { return (ICurve)c.ConvertToSpeckle(s); }).ToList();

      return myPoly;
    }

    public static PolyCurve ToNative(this Polycurve p)
    {
      PolyCurve myPolyc = new PolyCurve();
      foreach (var segment in p.segments)
      {
        try
        {
          //let the converter pick the best type of curve
          var c = new ConverterRhinoGh();
          myPolyc.AppendSegment((RH.Curve)c.ConvertToNative((Base)segment));
        }
        catch { }
      }

      if (p.domain != null)
        myPolyc.Domain = p.domain.ToNative();

      return myPolyc;
    }

    // Curve
    public static Base ToSpeckle(this NurbsCurve curve)
    {
      if (curve.IsArc(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
      {
        RH.Arc getObj;
        curve.TryGetArc(out getObj);
        Arc myObject = getObj.ToSpeckle();

        return myObject;
      }

      if (curve.IsCircle(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) && curve.IsClosed)
      {
        RH.Circle getObj;
        curve.TryGetCircle(out getObj);
        Circle myObject = getObj.ToSpeckle();

        return myObject;
      }

      if (curve.IsEllipse(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) && curve.IsClosed)
      {
        RH.Ellipse getObj;
        curve.TryGetEllipse(out getObj);
        Ellipse myObject = getObj.ToSpeckle();

        return myObject;
      }

      if (curve.IsLinear(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) || curve.IsPolyline()) // defaults to polyline
      {
        RH.Polyline getObj;
        curve.TryGetPolyline(out getObj);
        if (null != getObj)
        {
          Base myObject = getObj.ToSpeckle();

          return myObject;
        }
      }

      RH.Polyline poly;
      curve.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out poly);

      Polyline displayValue;

      if (poly.Count == 2)
      {
        displayValue = new Polyline();
        displayValue.value = new List<double> { poly[0].X, poly[0].Y, poly[0].Z, poly[1].X, poly[1].Y, poly[1].Z };
      }
      else
      {
        displayValue = poly.ToSpeckle() as Polyline;
      }

      Curve myCurve = new Curve(displayValue);
      NurbsCurve nurbsCurve = curve.ToNurbsCurve();

      myCurve.weights = nurbsCurve.Points.Select(ctp => ctp.Weight).ToList();
      myCurve.points = nurbsCurve.Points.Select(ctp => ctp.Location).ToFlatArray().ToList();
      myCurve.knots = nurbsCurve.Knots.ToList();
      myCurve.degree = nurbsCurve.Degree;
      myCurve.periodic = nurbsCurve.IsPeriodic;
      myCurve.rational = nurbsCurve.IsRational;
      myCurve.domain = nurbsCurve.Domain.ToSpeckle();
      myCurve.closed = nurbsCurve.IsClosed;

      return myCurve;
    }


    public static Curve ToSpeckleNurbs(this NurbsCurve curve)
    {
      var tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

      curve.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out var poly);

      Polyline displayValue;

      if (poly.Count == 2)
      {
        displayValue = new Polyline();
        displayValue.value = new List<double> {poly[0].X, poly[0].Y, poly[0].Z, poly[1].X, poly[1].Y, poly[1].Z};
      }
      else
      {
        displayValue = poly.ToSpeckle() as Polyline;
      }

      var myCurve = new Curve(displayValue);
      var nurbsCurve = curve.ToNurbsCurve();
    
      // Hack: Rebuild curve to prevent interior knot multiplicities.
      //var max = Math.Min(nurbsCurve.Points.Count-1, 3);
      //nurbsCurve = nurbsCurve.Rebuild(nurbsCurve.Points.Count, max, true);

      myCurve.weights = nurbsCurve.Points.Select(ctp => ctp.Weight).ToList();
      myCurve.points = nurbsCurve.Points.Select(ctp => ctp.Location).ToFlatArray().ToList();
      myCurve.knots = nurbsCurve.Knots.ToList();
      myCurve.degree = nurbsCurve.Degree;
      myCurve.periodic = nurbsCurve.IsPeriodic;
      myCurve.rational = nurbsCurve.IsRational;
      myCurve.domain = nurbsCurve.Domain.ToSpeckle();
      myCurve.closed = nurbsCurve.IsClosed;

      return myCurve;
    }

    public static NurbsCurve ToNative(this Curve curve)
    {
      var ptsList = curve.points.ToPoints();

      var nurbsCurve = NurbsCurve.Create(false, curve.degree, ptsList);

      for (int j = 0; j < nurbsCurve.Points.Count; j++)
      {
        nurbsCurve.Points.SetPoint(j, ptsList[j], curve.weights[j]);
      }

      for (int j = 0; j < nurbsCurve.Knots.Count; j++)
      {
        nurbsCurve.Knots[j] = curve.knots[j];
      }

      nurbsCurve.Domain = curve.domain.ToNative();
      return nurbsCurve;
    }

    // Box
    public static Box ToSpeckle(this RH.Box box)
    {
      var speckleBox = new Box(box.Plane.ToSpeckle(), box.X.ToSpeckle(), box.Y.ToSpeckle(), box.Z.ToSpeckle());
      return speckleBox;
    }

    public static RH.Box ToNative(this Box box)
    {
      return new RH.Box(box.basePlane.ToNative(), box.xSize.ToNative(), box.ySize.ToNative(), box.zSize.ToNative());
    }

    // Meshes
    public static Mesh ToSpeckle(this RH.Mesh mesh)
    {
      var verts = mesh.Vertices.ToPoint3dArray().ToFlatArray();

      var Faces = mesh.Faces.SelectMany(face =>
      {
        if (face.IsQuad) return new int[] { 1, face.A, face.B, face.C, face.D };
        return new int[] { 0, face.A, face.B, face.C };
      }).ToArray();

      var Colors = mesh.VertexColors.Select(cl => cl.ToArgb()).ToArray();

      return new Mesh(verts, Faces, Colors, null);
    }

    public static RH.Mesh ToNative(this Mesh mesh)
    {
      RH.Mesh m = new RH.Mesh();
      m.Vertices.AddVertices(mesh.vertices.ToPoints());

      int i = 0;

      while (i < mesh.faces.Count)
      {
        if (mesh.faces[i] == 0)
        { // triangle
          m.Faces.AddFace(new MeshFace(mesh.faces[i + 1], mesh.faces[i + 2], mesh.faces[i + 3]));
          i += 4;
        }
        else
        { // quad
          m.Faces.AddFace(new MeshFace(mesh.faces[i + 1], mesh.faces[i + 2], mesh.faces[i + 3], mesh.faces[i + 4]));
          i += 5;
        }
      }
      try
      {
        m.VertexColors.AppendColors(mesh.colors.Select(c => System.Drawing.Color.FromArgb((int)c)).ToArray());
      }
      catch { }

      if (mesh.textureCoordinates != null)
        for (int j = 0; j < mesh.textureCoordinates.Count; j += 2)
        {
          m.TextureCoordinates.Add(mesh.textureCoordinates[j], mesh.textureCoordinates[j + 1]);
        }

      return m;
    }

    // Breps
    public static Brep ToSpeckle(this RH.Brep brep)
    {
      var joinedMesh = new RH.Mesh();

      MeshingParameters mySettings;
      mySettings = new MeshingParameters(0);

      RH.Mesh.CreateFromBrep(brep, mySettings).All(meshPart => { joinedMesh.Append(meshPart); return true; });


      return new Brep(displayValue: joinedMesh.ToSpeckle(), rawData: JsonConvert.SerializeObject(brep), provenance: Speckle.Core.Kits.Applications.Rhino);
    }

    public static RH.Brep ToNative(this Brep brep)
    {
      try
      {
        if (brep.provenance == Speckle.Core.Kits.Applications.Rhino)
        {
          var myBrep = JsonConvert.DeserializeObject<RH.Brep>((string)brep.rawData);
          return myBrep;
        }
        throw new Exception("Unknown brep provenance: " + brep.provenance + ". Don't know how to convert from one to the other.");
      }
      catch
      {
        System.Diagnostics.Debug.WriteLine("Failed to deserialise brep");
        return null;
      }
    }

    // Extrusions
    // TODO: Research into how to properly create and recreate extrusions. Current way we compromise by transforming them into breps.
    public static Brep ToSpeckle(this Rhino.Geometry.Extrusion extrusion)
    {
      return extrusion.ToBrep().ToSpeckle();

      //var myExtrusion = new SpeckleExtrusion( SpeckleCore.Converter.Serialise( extrusion.Profile3d( 0, 0 ) ), extrusion.PathStart.DistanceTo( extrusion.PathEnd ), extrusion.IsCappedAtBottom );

      //myExtrusion.PathStart = extrusion.PathStart.ToSpeckle();
      //myExtrusion.PathEnd = extrusion.PathEnd.ToSpeckle();
      //myExtrusion.PathTangent = extrusion.PathTangent.ToSpeckle();

      //var Profiles = new List<SpeckleObject>();
      //for ( int i = 0; i < extrusion.ProfileCount; i++ )
      //  Profiles.Add( SpeckleCore.Converter.Serialise( extrusion.Profile3d( i, 0 ) ) );

      //myExtrusion.Profiles = Profiles;
      //myExtrusion.Properties = extrusion.UserDictionary.ToSpeckle( root: extrusion );
      //myExtrusion.GenerateHash();
      //return myExtrusion;
    }

    // TODO: See above. We're no longer creating new extrusions. This is here just for backwards compatibility.
    public static RH.Extrusion ToNative(this Extrusion extrusion)
    {
      RH.Curve outerProfile = ((Curve)extrusion.profile).ToNative();
      RH.Curve innerProfile = null;
      if (extrusion.profiles.Count == 2) innerProfile = ((Curve)extrusion.profiles[1]).ToNative();

      try
      {
        var IsClosed = extrusion.profile.GetType().GetProperty("IsClosed").GetValue(extrusion.profile, null) as bool?;
        if (IsClosed != true)
          outerProfile.Reverse();
      }
      catch { }

      var myExtrusion = RH.Extrusion.Create(outerProfile.ToNurbsCurve(), (double)extrusion.length, (bool)extrusion.capped);
      if (innerProfile != null)
        myExtrusion.AddInnerProfile(innerProfile);

      return myExtrusion;
    }

    //  Curve profile = null;
    //  try
    //  {
    //    var toNativeMethod = extrusion.Profile.GetType().GetMethod( "ToNative" );
    //    profile = ( Curve ) toNativeMethod.Invoke( extrusion.Profile, new object[ ] { extrusion.Profile } );
    //    if ( new string[ ] { "Polyline", "Polycurve" }.Contains( extrusion.Profile.Type ) )
    //      try
    //      {
    //        var IsClosed = extrusion.Profile.GetType().GetProperty( "IsClosed" ).GetValue( extrusion.Profile, null ) as bool?;
    //        if ( IsClosed != true )
    //        {
    //          profile.Reverse();
    //        }
    //      }
    //      catch { }


    //    //switch ( extrusion.Profile )
    //    //{
    //    //  case SpeckleCore.SpeckleCurve curve:
    //    //    profile = curve.ToNative();
    //    //    break;
    //    //  case SpeckleCore.SpecklePolycurve polycurve:
    //    //    profile = polycurve.ToNative();
    //    //    if ( !profile.IsClosed )
    //    //      profile.Reverse();
    //    //    break;
    //    //  case SpeckleCore.SpecklePolyline polyline:
    //    //    profile = polyline.ToNative();
    //    //    if ( !profile.IsClosed )
    //    //      profile.Reverse();
    //    //    break;
    //    //  case SpeckleCore.SpeckleArc arc:
    //    //    profile = arc.ToNative();
    //    //    break;
    //    //  case SpeckleCore.SpeckleCircle circle:
    //    //    profile = circle.ToNative();
    //    //    break;
    //    //  case SpeckleCore.SpeckleEllipse ellipse:
    //    //    profile = ellipse.ToNative();
    //    //    break;
    //    //  case SpeckleCore.SpeckleLine line:
    //    //    profile = line.ToNative();
    //    //    break;
    //    //  default:
    //    //    profile = null;
    //    //    break;
    //    //}
    //  }
    //  catch { }
    //  var x = new Extrusion();

    //  if ( profile == null ) return null;

    //  var myExtrusion = Extrusion.Create( profile.ToNurbsCurve(), ( double ) extrusion.Length, ( bool ) extrusion.Capped );

    //  myExtrusion.UserDictionary.ReplaceContentsWith( extrusion.Properties.ToNative() );
    //  return myExtrusion;
    //}

    // Proper explosion of polycurves:
    // (C) The Rutten David https://www.grasshopper3d.com/forum/topics/explode-closed-planar-curve-using-rhinocommon 
    public static bool CurveSegments(List<RH.Curve> L, RH.Curve crv, bool recursive)
    {
      if (crv == null) { return false; }

      PolyCurve polycurve = crv as PolyCurve;

      if (polycurve != null)
      {
        if (recursive) { polycurve.RemoveNesting(); }

        RH.Curve[] segments = polycurve.Explode();

        if (segments == null) { return false; }
        if (segments.Length == 0) { return false; }

        if (recursive)
        {
          foreach (RH.Curve S in segments)
          {
            CurveSegments(L, S, recursive);
          }
        }
        else
        {
          foreach (RH.Curve S in segments)
          {
            L.Add(S.DuplicateShallow() as RH.Curve);
          }
        }

        return true;
      }

      //Nothing else worked, lets assume it's a nurbs curve and go from there...
      NurbsCurve nurbs = crv.ToNurbsCurve();
      if (nurbs == null) { return false; }

      double t0 = nurbs.Domain.Min;
      double t1 = nurbs.Domain.Max;
      double t;

      int LN = L.Count;

      do
      {
        if (!nurbs.GetNextDiscontinuity(Continuity.C1_locus_continuous, t0, t1, out t)) { break; }

        RH.Interval trim = new RH.Interval(t0, t);
        if (trim.Length < 1e-10)
        {
          t0 = t;
          continue;
        }

        RH.Curve M = nurbs.DuplicateCurve();
        M = M.Trim(trim);
        if (M.IsValid) { L.Add(M); }

        t0 = t;
      } while (true);

      if (L.Count == LN) { L.Add(nurbs); }

      return true;
    }
  }
}