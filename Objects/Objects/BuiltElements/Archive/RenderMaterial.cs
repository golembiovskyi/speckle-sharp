﻿using Speckle.Core.Models;
using Speckle.Core.Kits;

using System.Drawing;
using System.Text;

namespace Objects.Other
{
  /// <summary>
  /// Minimal physically based material DTO class. Based on references from
  /// https://threejs.org/docs/index.html#api/en/materials/MeshStandardMaterial
  /// Theoretically has equivalents in Unity and Unreal.
  ///
  /// See: https://docs.unrealengine.com/en-US/RenderingAndGraphics/Materials/PhysicallyBased/index.html
  /// And: https://blogs.unity3d.com/2014/10/29/physically-based-shading-in-unity-5-a-primer/
  /// </summary>
  public class RenderMaterial : Base
  {
    public string name { get; set; }
    public double opacity { get; set; } = 1;
    public double metalness { get; set; } = 0;
    public double roughness { get; set; } = 1;
    public int diffuse { get; set; } = Color.LightGray.ToArgb();
    public int emissive { get; set; } = Color.Black.ToArgb();

    public RenderMaterial() { }

    [SchemaInfo("RenderMaterial", "Creates a render material.", "BIM", "Other")]
    public RenderMaterial(double opacity = 1, double metalness = 0, double roughness = 1, Color? diffuse = null, Color? emissive = null)
    {
      this.opacity = opacity;
      this.metalness = metalness;
      this.roughness = roughness;
      this.diffuse = (diffuse.HasValue) ? diffuse.Value.ToArgb() : Color.LightGray.ToArgb();
      this.emissive = (emissive.HasValue) ? emissive.Value.ToArgb() : Color.Black.ToArgb();
    }
  }
}