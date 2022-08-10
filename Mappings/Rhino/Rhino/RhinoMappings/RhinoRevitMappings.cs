using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MappingSchemas;

namespace SpeckleRhinoMappings
{
  public class RhinoRevitMappings
  {
    public Rhino2RevitSchemas RRM { get; set; }
    public RhinoRevitMappings()
    {
      RRM = new Rhino2RevitSchemas();
    }

    public List<Schema> GetSelectionSchemas(IEnumerable<RhinoObject> selection)
    {
      var result = new List<Schema>();
      var first = true;
      
      foreach(var obj in selection)
      {
        var schemas = GetObjectSchemas(obj);
        if(first)
        {
          result = schemas;
          first = false;
          continue;
        }
        result = result.Intersect(schemas).ToList();
      }
      if (result.Count == 0) return new List<Schema> { RRM.IncompatibleSelection };
      return result.ToList();
    }

    public List<Schema> GetObjectSchemas(RhinoObject obj)
    {
      var cats = new List<Schema>();

      switch (obj.Geometry)
      {
        case Mesh _m:
          cats.Add(RRM.DirectShape);
          // TODO: check if mesh is properly triagnualted, etc.
          // Set warnings if needed
          RRM.Topography.Errors = "Revit topogrhapies need to be triangulated. Your mesh has quads, you bastard!";
          cats.Add(RRM.Topography);
          break;

        case Brep b:
          if (b.IsSurface) cats.Add(RRM.DirectShape); // TODO: Wall by face, totally faking it right now
          else cats.Add(RRM.DirectShape);
          break;

        case Extrusion e:
          if (e.ProfileCount > 1) break;
          var crv = e.Profile3d(0, 0);
          if (!(crv.IsLinear() || crv.IsArc())) break;

          if (crv.PointAtStart.Z == crv.PointAtEnd.Z) break;
          
            cats.Add(RRM.Wall);
          break;

        case Curve c:
          if (c.IsLinear()) cats.Add(RRM.Beam);
          if (c.IsLinear() && c.PointAtEnd.Z == c.PointAtStart.Z) cats.Add(RRM.Gridline);
          if (c.IsLinear() && c.PointAtEnd.X == c.PointAtStart.X && c.PointAtEnd.Y == c.PointAtStart.Y) cats.Add(RRM.Column); // TODO: Slanted columns ?
          if (c.IsArc() && !c.IsCircle() && c.PointAtEnd.Z == c.PointAtStart.Z) cats.Add(RRM.Gridline);
          break;
      }

      return cats;
    }
  }


}
