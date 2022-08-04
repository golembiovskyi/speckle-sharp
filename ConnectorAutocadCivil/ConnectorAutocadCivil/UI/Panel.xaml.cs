using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.Windows;
using DesktopUI2.Views;

namespace Speckle.ConnectorAutocadCivil
{
  /// <summary>
  /// Interaction logic for Page1.xaml
  /// </summary>
  public partial class Panel
  {
    public static DockablePalette window = new DockablePalette();
    public static PaletteSet ps = null;

    public Panel()
    {
      if (ps == null)
      {
        ps = new PaletteSet("", new System.Guid(""));
        ps.Load += Ps_Load;
        ps.Save += Ps_Save;
        ps.AddVisual("", window);
        ps.KeepFocus = true;
        ps.Visible = true;

        InitializeComponent();
        AvaloniaHost.MessageHook += AvaloniaHost_MessageHook;
      }
      ps.Visible = true;
    }

    private static void Ps_Save(object sender, PalettePersistEventArgs e)
    {
      string position = Convert.ToString(e.ConfigurationSection.ReadProperty("Position", "Default"));
      switch (position)
      {
        case "Floating":
          double x = Convert.ToDouble(e.ConfigurationSection.ReadProperty("Left", 22.3));
          double y = Convert.ToDouble(e.ConfigurationSection.ReadProperty("Top", 22.3));
          double width = Convert.ToDouble(e.ConfigurationSection.ReadProperty("Width", 22.3));
          double height = Convert.ToDouble(e.ConfigurationSection.ReadProperty("Height", 22.3));
          ps.InitializeFloatingPosition(new System.Windows.Rect(x, y, width, height));
          break;
        case "Left":
          ps.Dock = Autodesk.AutoCAD.Windows.DockSides.Left;
          break;
        case "Right":
          ps.Dock = Autodesk.AutoCAD.Windows.DockSides.Right;
          break;
        default:
          ps.InitializeFloatingPosition(new System.Windows.Rect(100, 100, 100, 200));
          break;
      }
    }

    private static void Ps_Load(object sender, PalettePersistEventArgs e)
    {
      ps.Style = PaletteSetStyles.ShowCloseButton;
      if (!ps.Visible)
        e.ConfigurationSection.WriteProperty("Position", "Hidden");
      else
      {
        switch (ps.Dock)
        {
          case Autodesk.AutoCAD.Windows.Docksides.None:
            e.ConfigurationSection.WriteProperty("Position", "Floating");
            break;
          case Autodesk.AutoCAD.Windows.Docksides.Left:
            break;
          case Autodesk.AutoCAD.Windows.Docksides.Right:
            break;
          default:
            break;
        }
      }
    }

    private const UInt32 DLGC_WANTARROWS = 0x0001;
    private const UInt32 DLGC_HASSETSEL = 0x0008;
    private const UInt32 DLGC_WANTCHARS = 0x0080;
    private const UInt32 WM_GETDLGCODE = 0x0087;

    /// <summary>
    /// WPF was handling all the text input events and they where not being passed to the Avalonia control
    /// This ensures they are passed, see: https://github.com/AvaloniaUI/Avalonia/issues/8198#issuecomment-1168634451
    /// </summary>
    private IntPtr AvaloniaHost_MessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
      if (msg != WM_GETDLGCODE) return IntPtr.Zero;
      handled = true;
      return new IntPtr(DLGC_WANTCHARS | DLGC_WANTARROWS | DLGC_HASSETSEL);
    }

    /// <summary>
    /// Switching documents in Autocad causes the Panel content to "reset", so we need to re-initialize the avalonia host each time
    /// </summary>
    public void Init()
    {
      AvaloniaHost.Content = new MainUserControl();
    }
  }
}
