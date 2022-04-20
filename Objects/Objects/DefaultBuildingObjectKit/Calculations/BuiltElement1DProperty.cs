﻿using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.DefaultBuildingObjectKit.enums;

namespace Objects.DefaultBuildingObjectKit.Calculations
{
  public class BuiltElement1DProperty : Base
  {
    public string name { get; set; }
    public Element1DType element1DType { get; set; }
    public BuiltElement1DProfile builtElement1DProfile { get; set; }

    public Base parameters { get; set; } // should we do a dictionary thing tbh ? 
  }
}