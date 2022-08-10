using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace SpeckleRhinoMappings
{
  public class SpeckleRhinoMappingsCommand : Command
  {
    public SpeckleRhinoMappingsCommand()
    {
      Instance = this;
    }

    public static SpeckleRhinoMappingsCommand Instance { get; private set; }

    public override string EnglishName => "SpeckleRhinoMappings";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      RhinoWindows.Controls.DockBar.Show(WV2DockBar.BarId, false);
      return Result.Success;
    }
  }
}
