﻿using System;
using System.Collections.Generic;
using System.Text;
using Objects.Building.enums;
using Speckle.Core.Models;

namespace Objects.Definitions
{
  public class BaseCurveProperty : Base
  {
    public string name { get; set; }
    //public double thickness { get; set; }

    public CurveElementType type { get; set; }
  }
}