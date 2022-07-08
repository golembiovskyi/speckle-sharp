using System;
using System.Text;
using System.Collections.Generic;
using ConverterRevitShared.Revit;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using System.IO;
using System.Linq;
using Speckle.Core.Models;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    /// <summary>
    /// The current document of the application
    /// </summary>
    private static Autodesk.Revit.DB.Document doc;

    private ViewPlan planView = null;
    private ViewSection profileSection = null;
    private SketchPlane sketchPlane = null;
    private Line dimLine;

    private List<XYZ> points = new List<XYZ>();

    #region replaceMeForNewShape
    //private static string familyName = "Wide Flange AISC (from Speckle)";
    private static string currentTypeName = "W12x19";
    private static string templateName = "Structural Framing - Beams and Braces";
    double conversion; // conversion to native units
    double b; //width
    double d; //height
    double k; // top of top flange to end of fillet
    double tf; //flange thickness
    double tw; //web thickness

    private void definePoints()
    {
      points = new List<XYZ>
      {
        new XYZ(0, b / 2, d / 2),
        new XYZ(0, -b / 2, d / 2),
        new XYZ(0, -b / 2, d / 2 - tf),
        new XYZ(0, -(tw / 2 + k - tf), d / 2 - tf),
        new XYZ(0, -tw / 2, d / 2 - k),
        new XYZ(0, -tw / 2, -(d / 2 - k)),
        new XYZ(0, -(tw / 2 + k - tf), -(d / 2 - tf)),
        new XYZ(0, -b / 2, -(d / 2 - tf)),
        new XYZ(0, -b / 2, -d / 2),
        new XYZ(0, b / 2, -d / 2),
        new XYZ(0, b / 2, -(d / 2 - tf)),
        new XYZ(0, (tw / 2 + k - tf), -(d / 2 - tf)),
        new XYZ(0, tw / 2, -(d / 2 - k)),
        new XYZ(0, tw / 2, d / 2 - k),
        new XYZ(0, (tw / 2 + k - tf), d / 2 - tf),
        new XYZ(0, b / 2, d / 2 - tf)
      };
    }

    private List<List<string>> equalSpacingConstraints = new List<List<string>>
    {
      //new List<string> { "15", "OriginY", "1" },
      //new List<string> { "9", "OriginY", "7" },
      //new List<string> { "12", "OriginY", "4" },
      //new List<string> { "0", "OriginZ", "8" },
      //new List<string> { "14", "OriginZ", "10" },
      //new List<string> { "2", "OriginZ", "6" },
      new List<string> { "OriginY", "leftOfFlange", "rightOfFlange" },
      new List<string> { "OriginY", "leftOfFillet", "rightOfFillet" },
      new List<string> { "OriginY", "leftOfWeb", "rightOfWeb" },
      new List<string> { "OriginZ", "topOfTopFlange", "bottomOfBottomFlange" },
      new List<string> { "OriginZ", "bottomOfTopFlange", "topOfBottomFlange" },
      new List<string> { "OriginZ", "topOfWeb", "bottomOfWeb" },
    };

    private List<List<string>> alignmentConstraints = new List<List<string>>
    {
      //new List<string> { "leftOfFlange", "15" },
      //new List<string> { "leftOfFlange", "9" },
      //new List<string> { "leftOfWeb", "12" },
      //new List<string> { "rightOfWeb", "4" },
      //new List<string> { "rightOfFlange", "1" },
      //new List<string> { "rightOfFlange", "7" },
      //new List<string> { "topOfTopFlange", "0" },
      //new List<string> { "bottomOfTopFlange", "14" },
      //new List<string> { "bottomOfTopFlange", "2" },
      //new List<string> { "topOfBottomFlange", "10" },
      //new List<string> { "topOfBottomFlange", "6" },
      //new List<string> { "bottomOfBottomFlange", "9" }
      new List<string> { "memberLeft", }
    };

    private List<List<string>> shapeDimensions = new List<List<string>>
    {
      new List<string> { "b", "leftOfFlange", "rightOfFlange" },
      new List<string> { "d", "topOfTopFlange", "bottomOfBottomFlange" },
      new List<string> { "k", "topOfTopFlange", "topOfWeb" },
      new List<string> { "tf", "topOfTopFlange", "bottomOfTopFlange" },
      new List<string> { "tw", "leftOfWeb", "rightOfWeb" },
    };
    #endregion


    /// <summary>
    /// Implement this method as an external command for Revit.
    /// </summary>
    /// <param name="revit">An object that is passed to the external application 
    /// which contains data related to the command, 
    /// such as the application object and active view.</param>
    /// <param name="message">A message that can be set by the external application 
    /// which will be displayed if a failure or cancellation is returned by 
    /// the external command.</param>
    /// <param name="elements">A set of elements to which the external application 
    /// can add elements that are to be highlighted in case of failure or cancellation.</param>
    /// <returns>Return the status of the external command. 
    /// A result of Succeeded means that the API external method functioned as expected. 
    /// Cancelled can be used to signify that the user cancelled the external operation 
    /// at some point. Failure should be returned if the application is unable to proceed with 
    /// the operation.</returns>
 
    public FamilySymbol CreateWideFlangeType(Base element)
    {
      // is family already loaded into project?
      //option #1 - normal fam name
        //fam in prject

        //fam not in project

      //option #2 - speckSuffix
        //fam in prject

        //fam not in project

      string famNameWithSuffix = FamNameWithSuffix(element);
      string typeName = element["type"] as string;

      List<FamilySymbol> familySymbols = new List<FamilySymbol>();
      Family existingFamily = null;
      ElementFilter filter = GetCategoryFilter(element);

      if (filter != null)
      {
        familySymbols = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(FamilySymbol)).WherePasses(filter).ToElements().Cast<FamilySymbol>().ToList();
      }
      else
      {
        familySymbols = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(FamilySymbol)).ToElements().Cast<FamilySymbol>().ToList();
      }
      foreach (FamilySymbol familySymbol in familySymbols)
      {
        if (familySymbol.Family.Name == famNameWithSuffix)
        {
          existingFamily = familySymbol.Family;
          break;
        }
      }



      if (existingFamily == null)
      {
        existingFamily = CreateWideFlangeFamily(famNameWithSuffix);
      }
      if (existingFamily == null)
        return null;

      else
        return GetSymbol(existingFamily, typeName);
    }

    public string FamNameWithSuffix(Base element)
    {
      if (element["family"] is string famName)
      {
        if (famName.Substring(Math.Max(0, famName.Length - customClassSuffix.Length)) == customClassSuffix)
          return famName;

        return famName + customClassSuffix;
      }
      return null;
    }
    public Family CreateWideFlangeFamily(string familyName)
    {
      Report.Log($"CreateWideFlangeFamily");
      try
      {
        var tempPath = GetTemplatePath(templateName);
        doc = Doc.Application.NewFamilyDocument("C:\\ProgramData\\Autodesk\\RVT 2022\\Family Templates\\English-Imperial\\Structural Framing - Beams and Braces.rft");
        
        if (!doc.IsFamilyDocument
            || doc.OwnerFamily.FamilyCategory.Id.IntegerValue != (int)BuiltInCategory.OST_StructuralFraming)
        {
          //message = "Cannot execute wide flange creation in non-structuralFraming family document";
          return null;
        }

        Transaction tran = new Transaction(doc);
        Transaction newTran = new Transaction(doc);
        tran.Start($"Create Family: {familyName}");

        doc.OwnerFamily.Name = familyName;
        doc.FamilyManager.RenameCurrentType(currentTypeName);
        SetSketchPlane();
        Dictionary<string, ReferencePlane> refPlanes = GetExistingReferencePlanes();
        refPlanes = CreateReferencePlanes(refPlanes);
        Extrusion profileExtrusion = CreateExtrusion(refPlanes);
        AlignFaces(profileExtrusion, refPlanes);
        CreateEqConstraints(refPlanes);
        CreateDimensions(refPlanes);

        tran.Commit();

        return GetFamily(familyName);
      }
      catch (Exception ex)
      {
        Report.Log($"Exception {ex}");
        return null;
      }
    }

    private Dictionary<string, ReferencePlane> GetExistingReferencePlanes()
    {
      Dictionary<string, ReferencePlane> result = new Dictionary<string, ReferencePlane>();

      ElementClassFilter rp_filt = new ElementClassFilter(typeof(ReferencePlane));
      FilteredElementCollector rp_col = new FilteredElementCollector(doc);
      IList<Element> els = rp_col.WherePasses(rp_filt).ToElements();
      foreach (var el in els)
      {
        if (el is ReferencePlane rp)
        {
          IList<Parameter> definesOrigin = rp.GetParameters("Defines Origin");
          if (definesOrigin.Count > 0)
          {
            if (definesOrigin[0].AsInteger() == 1)
            {
              Plane plane = rp.GetPlane();
              rp.Pinned = true;
              if (Math.Abs(plane.Normal.X) == 1.0)
                result["OriginX"] = rp;
              else if (Math.Abs(plane.Normal.Y) == 1.0)
                result["OriginY"] = rp;
              else if (Math.Abs(plane.Normal.Z) == 1.0)
                result["OriginZ"] = rp;
            }
#if REVIT2022
            else if (rp.GetParameter(ParameterTypeId.ElemReferenceName).AsValueString() == "Member Left")
            {
              result["memberLeft"] = rp;
            }
            else if (rp.GetParameter(ParameterTypeId.ElemReferenceName).AsValueString() == "Member Right")
            {
              result["memberRight"] = rp;
            }
#endif
          }
        }
      }

      ElementClassFilter filt = new ElementClassFilter(typeof(ViewPlan));
      FilteredElementCollector col = new FilteredElementCollector(doc);
      IList<Element> plans = col.WherePasses(filt).ToElements();

      foreach (var view in plans)
      {
        if (view.GetParameters("Type")[0].AsValueString() == "Floor Plan")
        {
          planView = (ViewPlan)view;
        }
      }

      if (planView == null)
      {
        System.Diagnostics.Debug.WriteLine("section named left could not be found");
      }

      return result;
    }

    private void SetSketchPlane()
    {
      ElementClassFilter filt = new ElementClassFilter(typeof(Extrusion));
      FilteredElementCollector col = new FilteredElementCollector(doc);
      IList<Element> els = col.WherePasses(filt).ToElements();

      if (els.Count == 1)
      {
        sketchPlane = ((Extrusion)els.First()).Sketch.SketchPlane;
        doc.Delete(((Extrusion)els.First()).Id); // delete the original extrusion
      }
    }

    private Dictionary<string, ReferencePlane> CreateReferencePlanes(Dictionary<string, ReferencePlane> refPlanes)
    {

      ElementClassFilter filt = new ElementClassFilter(typeof(ViewSection));
      ElementClassFilter level_filt = new ElementClassFilter(typeof(ViewSection));
      FilteredElementCollector col = new FilteredElementCollector(doc);
      IList<Element> els = col.WherePasses(filt).ToElements();

      foreach (var sectionView in els)
      {
        if (sectionView.Name == "Left")
        {
          profileSection = (ViewSection)sectionView;
        }
      }

      if (profileSection == null)
      {
        System.Diagnostics.Debug.WriteLine("section named left could not be found");
      }


      conversion = .083333333; // conversion from inches to feet

      // vars
      b = conversion * 4.01; //width
      d = conversion * 12.2; //height
      k = conversion * .65; // top of top flange to end of fillet
      tf = conversion * .350; //flange thickness
      tw = conversion * .235; //web thickness

      // top of top flange
      XYZ bub = new XYZ(0, .75, d / 2);
      XYZ free = new XYZ(0, -.75, d / 2);
      XYZ third = new XYZ(1, 0, d / 2);
      ReferencePlane topOfTopFlange = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["topOfTopFlange"] = topOfTopFlange;
      topOfTopFlange.Name = "topOfTopFlange";

      // bottom of top flange
      bub = new XYZ(0, .75, d / 2 - tf);
      free = new XYZ(0, -.75, d / 2 - tf);
      third = new XYZ(1, 0, d / 2 - tf);
      ReferencePlane bottomOfTopFlange = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["bottomOfTopFlange"] = bottomOfTopFlange;
      bottomOfTopFlange.Name = "bottomOfTopFlange";

      // top of web
      bub = new XYZ(0, .75, d / 2 - k);
      free = new XYZ(0, -.75, d / 2 - k);
      third = new XYZ(1, 0, d / 2 - k);
      ReferencePlane topOfWeb = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["topOfWeb"] = topOfWeb;
      topOfWeb.Name = "topOfWeb";

      // bottom of web
      bub = new XYZ(0, .75, -(d / 2 - k));
      free = new XYZ(0, -.75, -(d / 2 - k));
      third = new XYZ(1, 0, -(d / 2 - k));
      ReferencePlane bottomOfWeb = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["bottomOfWeb"] = bottomOfWeb;
      bottomOfWeb.Name = "bottomOfWeb";

      // top of bottom flange
      bub = new XYZ(0, .75, -(d / 2 - tf));
      free = new XYZ(0, -.75, -(d / 2 - tf));
      third = new XYZ(1, 0, -(d / 2 - tf));
      ReferencePlane topOfBottomFlange = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["topOfBottomFlange"] = topOfBottomFlange;
      topOfBottomFlange.Name = "topOfBottomFlange";

      // bottom of bottom flange
      bub = new XYZ(0, .75, -d / 2);
      free = new XYZ(0, -.75, -d / 2);
      third = new XYZ(1, 0, -d / 2);
      ReferencePlane bottomOfBottomFlange = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["bottomOfBottomFlange"] = bottomOfBottomFlange;
      bottomOfBottomFlange.Name = "bottomOfBottomFlange";

      // left side of flange
      bub = new XYZ(0, b / 2, 1.92);
      free = new XYZ(0, b / 2, -1.92);
      third = new XYZ(1, b / 2, 0);
      ReferencePlane leftOfFlange = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["leftOfFlange"] = leftOfFlange;
      leftOfFlange.Name = "leftOfFlange";

      // end of fillet left of web
      bub = new XYZ(0, k - tf + tw / 2, 1.92);
      free = new XYZ(0, k - tf + tw / 2, -1.92);
      third = new XYZ(1, k - tf + tw / 2, 0);
      ReferencePlane leftOfFillet = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["leftOfFillet"] = leftOfFillet;
      leftOfFillet.Name = "leftOfFillet";

      // left end of web
      bub = new XYZ(0, tw / 2, 1.92);
      free = new XYZ(0, tw / 2, -1.92);
      third = new XYZ(1, tw / 2, 0);
      ReferencePlane leftOfWeb = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["leftOfWeb"] = leftOfWeb;
      leftOfWeb.Name = "leftOfWeb";

      // right end of web
      bub = new XYZ(0, -tw / 2, 1.92);
      free = new XYZ(0, -tw / 2, -1.92);
      third = new XYZ(1, -tw / 2, 0);
      ReferencePlane rightOfWeb = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["rightOfWeb"] = rightOfWeb;
      leftOfWeb.Name = "rightOfWeb";

      // end of fillet right of web
      bub = new XYZ(0, -(k - tf + tw / 2), 1.92);
      free = new XYZ(0, -(k - tf + tw / 2), -1.92);
      third = new XYZ(1, -(k - tf + tw / 2), 0);
      ReferencePlane rightOfFillet = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["rightOfFillet"] = rightOfFillet;
      leftOfFillet.Name = "rightOfFillet";

      // right side of flange
      bub = new XYZ(0, -b / 2, 1.92);
      free = new XYZ(0, -b / 2, -1.92);
      third = new XYZ(1, -b / 2, 0);
      ReferencePlane rightOfFlange = doc.FamilyCreate.NewReferencePlane2(bub, free, third, profileSection);
      refPlanes["rightOfFlange"] = rightOfFlange;
      leftOfFlange.Name = "rightOfFlange";

      return refPlanes;
    }

    private Extrusion CreateExtrusion(Dictionary<string, ReferencePlane> refPlanes)
    {
      ElementClassFilter filter = new ElementClassFilter(typeof(ViewSection));
      ElementClassFilter refPlaneFilter = new ElementClassFilter(typeof(ReferencePlane));
      FilteredElementCollector col = new FilteredElementCollector(doc);
      IList<Element> els = col.WherePasses(filter).ToElements();

      definePoints();

      // create curve array
      CurveArray curves = new CurveArray();

      //sketchPlane = profileSection.SketchPlane;
      sketchPlane = SketchPlane.Create(doc, refPlanes["OriginX"].GetPlane());
      System.Diagnostics.Debug.WriteLine($"sketchPlane {sketchPlane}");

      for (int i = 0; i < points.Count - 1; i++)
      {
        //if (i == 3 || i == 5 || i == 10 || i == 12)
        //{
        //  curves.Append(new Arc.Create())
        //}
        curves.Append(Line.CreateBound(points[i], points[i + 1]));
      }
      curves.Append(Line.CreateBound(points.Last(), points[0]));

      CurveArrArray profile = new CurveArrArray();
      profile.Append(curves);

      Extrusion extrusion = doc.FamilyCreate.NewExtrusion(true, profile, sketchPlane, 8);
      extrusion.Location.Move(new XYZ(-4, 0, 0));

      FamilyElementVisibility vis = new FamilyElementVisibility(FamilyElementVisibilityType.Model);
      vis.IsShownInCoarse = false;

      extrusion.SetVisibility(vis);

      return extrusion;
    }

    private void AlignFaces(Extrusion ex, Dictionary<string, ReferencePlane> refPlanes)
    {
      Options op = new Options();
      op.ComputeReferences = true;
      GeometryElement geomObjs = ex.get_Geometry(op);

      // loop through the array and find a face with the given normal
      //
      foreach (GeometryObject geomObj in geomObjs)
      {
        bool leftNeedsAlign = true;
        bool rightNeedsAlign = true;
        if (geomObj is Solid eSolid) // solid is what we are interested in.
        {
          EdgeArray edges = eSolid.Edges;
          foreach (Edge edge in edges)
          {
            if (edge.AsCurve() is Line edgeLine)
            {
              if (edgeLine.GetEndPoint(0).X == -4 && edgeLine.GetEndPoint(1).X == -4 && leftNeedsAlign)
              {
                doc.FamilyCreate.NewAlignment(planView, edge.Reference, refPlanes["memberLeft"].GetReference());
                leftNeedsAlign = false;
              }
              else if (edgeLine.GetEndPoint(0).X == 4 && edgeLine.GetEndPoint(1).X == 4 && rightNeedsAlign)
              {
                doc.FamilyCreate.NewAlignment(planView, edge.Reference, refPlanes["memberRight"].GetReference());
                rightNeedsAlign = false;
              }
            }
          }
        }
      }
    }

    private void CreateEqConstraints(Dictionary<string, ReferencePlane> refPlanes)
    {
      foreach (var elements in equalSpacingConstraints)
      {
        ReferenceArray refs = new ReferenceArray();
        foreach (var el in elements)
        {
          refs.Append(GetRefPlaneReference(el, refPlanes));
        }
        Line dimLine = CreateLineFromReferencePlane(elements[0], refPlanes);
        Dimension dim = doc.FamilyCreate.NewDimension(profileSection, dimLine, refs);
        dim.AreSegmentsEqual = true;
      }
    }

    private Reference GetRefPlaneReference(string s, Dictionary<string, ReferencePlane> refPlanes)
    {
      if (refPlanes.ContainsKey(s))
      {
        return refPlanes[s].GetReference();
      }
      return null;
    }

    private Line CreateLineFromReferencePlane(string s, Dictionary<string, ReferencePlane> refPlanes)
    {
      if (refPlanes.ContainsKey(s))
      {
        Plane plane = refPlanes[s].GetPlane();
        return Line.CreateUnbound(plane.Origin, plane.Normal);
      }
      return null;
    }

    private void CreateDimensions(Dictionary<string, ReferencePlane> refPlanes)
    {
      foreach (var dimension in shapeDimensions)
      {
        // define parameter
#if REVIT2022
        FamilyParameter param = doc.FamilyManager.AddParameter(dimension[0], GroupTypeId.Geometry, SpecTypeId.SectionProperty, false);
        if (param.Definition.Name != "Length (default)")
        {

        }

        // create dimension
        ReferenceArray refs = new ReferenceArray();
        refs.Append(GetRefPlaneReference(dimension[1], refPlanes));
        refs.Append(GetRefPlaneReference(dimension[2], refPlanes));

        Line dimLine = CreateLineFromReferencePlane(dimension[1], refPlanes);
        Dimension dim = doc.FamilyCreate.NewDimension(profileSection, dimLine, refs);

        // bind parameter to dimension
        dim.FamilyLabel = param;
#endif
      }
    }
    private Family GetFamily(string famName)
    {
      string tempFamilyPath = Path.Combine(Path.GetTempPath(), famName + ".rfa");
      var so = new SaveAsOptions();
      so.OverwriteExistingFile = true;
      doc.SaveAs(tempFamilyPath, so);
      doc.Close();

      Doc.LoadFamily(tempFamilyPath, new FamilyLoadOption(), out var fam);
      try
      {
        File.Delete(tempFamilyPath);
      }
      catch
      {
      }
      return fam;
    }
    private FamilySymbol GetSymbol(Family family, string type)
    {
      FamilySymbol returnSymbol = null;
      foreach (var symbolId in family.GetFamilySymbolIds())
      {
        var symbol = Doc.GetElement(symbolId) as FamilySymbol;
        if (symbol.Name == type)
        {
          returnSymbol = symbol;
          break;
        }
      }
      //var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as FamilySymbol;
      returnSymbol.Activate();
     
      return returnSymbol;
    }
  }
}
