﻿using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.Building;
using Objects.Definitions;

namespace Objects.Building
{
  public class NonPlanarRoof : CurveBasedElement
  {

    // to implement source app parameters interface from claire
    public double volume { get; set; }
    public double surfaceArea { get; set; }

  }
}