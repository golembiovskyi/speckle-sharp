using System;
using System.Collections.Generic;
using System.Text;

namespace MappingSchemas
{

  public class Schema
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public string Errors { get; set; }
    public Type SpeckleClass { get; set; } // TODO: Keep track of the basic relation the original class

    public List<object> Params { get; set; } // NOTE: because FML, see: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism

    // NOTE: used for hash sets
    public override int GetHashCode()
    {
      return Name.GetHashCode();
    }

    // NOTE: used(?) for linq intersection
    public override bool Equals(object obj)
    {
      if (obj is Schema s) return s.Name == Name;
      return false;
    }

  }

  public class SchemaParam
  {
    public string Name { get; set; } // "Bottom Offset" 
    public string ApplicationName { get; set; } // "RVT_BS_BOTTOM___@WELOVEJERE<Y
    public string Description { get; set; }

    public string Type { get; set; }

    public SchemaParam()
    {
      Type = GetType().Name;
    }
  }

  public class StringParam : SchemaParam
  {
    public string Value { get; set; }
  }

  public class DoubleParam : SchemaParam
  {
    public double Value { get; set; } = 0;
  }

  public class MultiselectParam : SchemaParam
  {
    public List<string> Values { get; set; }
    public string SelectedValue { get; set; }
  }

  public class CheckboxParam : SchemaParam
  {
    public bool Value { get; set; } = true;
  }
}
