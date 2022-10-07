﻿using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Objects.Organization
{
  public class NetworkLink : Base
  {
    public string name { get; set; }

    /// <summary>
    /// The index of the elements in <see cref="network"/> that are connected by this link
    /// </summary>
    public List<int> elementIndices { get; set; }

    [JsonIgnore] public Network network { get; set; }

    public NetworkLink() { }

    /// <summary>
    /// Retrieves the elements for this link
    /// </summary>
    [JsonIgnore] public List<NetworkElement> elements => elementIndices.Select(i => network.elements[i]).ToList();
  }
}
namespace Objects.Organization.Revit
{
  public class RevitNetworkLink : NetworkLink
  {
    /// <summary>
    /// The shape of the <see cref="NetworkLink"/>
    /// </summary>
    public NetworkLinkShape shape { get; set; }

    public Point origin { get; set; }

    public int connectionIndex { get; set; }

    public Vector direction { get; set; }

    public bool connectedToCurve { get; set; }

    public NetworkLinkDomain domain { get; set; }

    public bool connected { get; set; }

    public double height { get; set; }

    public double width { get; set; }

    public double diameter { get; set; }

    /// <summary>
    /// The system type
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// The system category
    /// </summary>
    public string category { get; set; }
  }

  /// <summary>
  /// Represents the shape of a <see cref="NetworkLink"/>.
  /// </summary>
  public enum NetworkLinkShape
  {
    Unknown = -1,
    Round = 0,
    Rectangular = 1,
    Oval = 2
  }

  /// <summary>
  /// Represents the connector domain of a <see cref="NetworkLink"/>.
  /// </summary>
  public enum NetworkLinkDomain
  {
    Unknown = 0,
    Duct = 1,
    Conduit = 2,
    Piping = 3,
    CableTray = 4
  }
}