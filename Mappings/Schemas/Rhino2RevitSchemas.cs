using System;
using System.Collections.Generic;

namespace MappingSchemas
{
  public class Rhino2RevitSchemas
  {
    public Schema Floor { get; set; } = new Schema
    {
      Name = "Floor",
      Description = "Creates Revit floors from planar horizontal surfaces",
      SpeckleClass = typeof(object),
      Params = new List<object>
        {
          new MultiselectParam { Name = "Family", Values = new List<string> {"Foo", "Bar", "Baz" } },
          new MultiselectParam { Name = "Type", Values = new List<string> {"A", "B", "C", "D" } },
          new CheckboxParam { Name = "Structural", Value = true, Description = "Defines this element as load bearing." },
          new StringParam { Name = "Comments" }
        }
    };

    public Schema Wall { get; set; } = new Schema
    {
      Name = "Wall",
      Description = "Creates Revit walls from planar extrusions.",
      Params = new List<object>
        {
          new MultiselectParam { Name = "Family", Values = new List<string> { "Foo", "Bar", "Baz" } },
          new MultiselectParam { Name = "Type", Values = new List<string> { "A", "B", "C", "D" } },
          new DoubleParam { Name = "bottom offset", Value = 0 },
          new DoubleParam { Name = "top offset", Value = 0 },
          new CheckboxParam { Name = "Structural", Value = true, Description = "Does this wall make the building stand up? In Revit, ofc." },
          new StringParam { Name = "Comments" }
        }
    };

    public Schema DirectShape { get; set; } = new Schema
    {
      Name = "DirectShape",
      Params = new List<object>
        {
          new MultiselectParam { Name = "Type", Values = new List<string> {"Floor", "Wall", "Roof", "Column" } }, // Competitors seems to have this functionality
          new CheckboxParam { Name = "Smooth Import", Description = "Elements will look better, but will take longer to create." },
          new StringParam { Name = "Comments" }
        }
    };

    public Schema Column = new Schema
    {
      Name = "Column",
      Params = new List<object>
        {
          new MultiselectParam { Name = "Family Instance", Values = new List<string> { "C 123", "C 10x10", "C 20x20", "L 20x40" } },
          new CheckboxParam { Name = "Structural", Value = true, Description = "Defines this element as load bearing." },
          new StringParam { Name = "Comments" }
        }
    };

    public Schema Beam = new Schema
    {
      Name = "Beam",
      Params = new List<object>
        {
          new MultiselectParam { Name = "Type", Values = new List<string> {"W 123", "W 10x10", "FOO 20x20", "STEEL 20x40" } },
          new DoubleParam { Name = "bottom offset", Value = 0 },
          new DoubleParam { Name = "top offset", Value = 0 },
          new StringParam { Name = "Comments" }
        }
    };

    public Schema Topography = new Schema
    {
      Name = "Topography",
      Params = new List<object>
      {
        new MultiselectParam { Name = "Type", Values = new List<string> { "Totally", "Fake " } },
        new MultiselectParam { Name = "Category", Values = new List<string> { "Also", "Totally", "Fake" } },
      }
    };

    public Schema Gridline = new Schema
    {
      Name = "Gridline"
    };

    public Schema IncompatibleSelection = new Schema
    {
      Name = "Incompatible Selection",
      Description = "Current selection objects cannot be assigned one single schema."
    };
  }
}
