﻿using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Building.enums;
using Objects.Geometry;

namespace Objects.Organization
{
  public class Axis : Base
  {
    public string name { get; set; }
    public AxisType axisType { get; set; }
    public Plane definition { get; set; }
    public Axis() { }

    [SchemaInfo("Axis", "Creates a Speckle axis (a user-defined axis)", "Structural", "Geometry")]
    public Axis(string name, AxisType axisType = AxisType.Cartesian, Plane definition = null)
    {
      this.name = name;
      this.axisType = axisType;
      this.definition = definition;
    }
  }

  public enum AxisType
  {
    Cartesian,
    Cylindrical,
    Spherical
  }
}