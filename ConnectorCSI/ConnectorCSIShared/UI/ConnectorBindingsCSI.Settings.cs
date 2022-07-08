using System.Collections.Generic;
using System.Linq;
using DesktopUI2.Models.Settings;

namespace Speckle.ConnectorCSI.UI
{
  public partial class ConnectorBindingsCSI
  {
    public override List<ISetting> GetSettings()
    {
      List<string> prettyMeshOptions = new List<string>() { "Merge by Level", "Merge by Plane X", "Merge by Plane Y" };


      return new List<ISetting>
      {     
      new CheckBoxSetting{Slug = "send-analysis-model-results", Name = "Send Results from Analysis Model", Icon = "Link", IsChecked = false, Description = "Include "}
      new ListBoxSetting {Slug = "recieve-merge-nodes", Name = "Merge Nodes", Icon ="ChartTimelineVarient", Values = prettyMeshOptions, Description = "Determines the display style of imported meshes"}
      };
    }
  }
}
