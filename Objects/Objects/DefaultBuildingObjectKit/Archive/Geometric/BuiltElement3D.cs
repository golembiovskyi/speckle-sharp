﻿using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Objects.BuiltElements.Revit.Curve;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
    public class BuiltElement3D : Base, IHasArea, IHasVolume, IDisplayMesh, IDisplayValue<List<Mesh>>
    {
        public string name { get; set; }
        public string number { get; set; }
        public double area { get; set; }
        public double volume { get; set; }
        public Point basePoint { get; set; }
        public Level level { get; set; }
        public double baseOffset { get; set; } = 0;
        public Level topLevel { get; set; } // corresponds to UpperLimit property in Revit api
        public double topOffset { get; set; } = 0; // corresponds to LimitOffset property in Revit api
        public List<ICurve> voids { get; set; } = new List<ICurve>();
        public ICurve outline { get; set; }
        public string spaceType { get; set; }
        public string zoneName { get; set; } 

        // additional properties to add: also inclue space separation lines here? Phase? Associated Room? Zone object instead of id?
        
        [DetachProperty]
        public List<Mesh> displayValue { get; set; }
        
        public string units { get; set; }
        public BuiltElement3D() { }

        [SchemaInfo("Space", "Creates a Speckle space", "BIM", "MEP")]
        public BuiltElement3D(string name, string number, [SchemaMainParam] Point basePoint, Level level)
        {
            this.name = name;
            this.number = number;
            this.basePoint = basePoint;
            this.level = level;
        }

        [SchemaInfo("Space with top level and offset parameters", "Creates a Speckle space with the specified top level and offsets", "BIM", "MEP")]
        public BuiltElement3D(string name, string number, [SchemaMainParam] Point basePoint, Level level, Level topLevel, double topOffset, double baseOffset)
        {
            this.name = name;
            this.number = number;
            this.basePoint = basePoint;
            this.level = level;
            this.topLevel = topLevel;
            this.topOffset = topOffset;
            this.baseOffset = baseOffset;
        }

    /// <summary>
    /// SchemaBuilder constructor for a Room
    /// </summary>
    /// <remarks>Assign units when using this constructor due to <paramref name="height"/> param</remarks>
    [SchemaInfo("Room", "Creates a Speckle room", "BIM", "Architecture")]
    public BuiltElement3D(string name, string number, Level level, [SchemaMainParam] Point basePoint)
    {
      this.name = name;
      this.number = number;
      this.level = level;
      this.basePoint = basePoint;
    }

    /// <summary>
    /// SchemaBuilder constructor for a Room
    /// </summary>
    /// <remarks>Assign units when using this constructor due to <paramref name="height"/> param</remarks>
    [SchemaInfo("RevitRoom", "Creates a Revit room with parameters", "Revit", "Architecture")]
    public Room(string name, string number, Level level, [SchemaMainParam] Point basePoint, List<Parameter> parameters = null)
    {
      this.name = name;
      this.number = number;
      this.level = level;
      this.basePoint = basePoint;
      this["parameters"] = parameters.ToBase();
    }

    }
}
