using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Web;

namespace Kcesar.MissionLine.Website.Data
{
  public class Carpooler
  {
    [Key, Column(Order = 0)]
    public int EventId { get; set; }

    [Key, Column(Order = 1)]
    public string MemberId { get; set; }

    /// <summary>
    /// A member can choose to be a driver, or driver and passenger
    /// </summary>
    public bool CanBeDriver { get; set; }

    /// <summary>
    /// A member can choose to be a passenger, or passenger and driver
    /// </summary>
    public bool CanBePassenger { get; set; }

    // Would use DbGeography spacial type, but it failed because Microsoft.SqlServer.Types version 10 or higher could not be found, and installing the NuGet package didn't work
    public decimal LocationLatitude { get; set; }

    public decimal LocationLongitude { get; set; }

    /// <summary>
    /// Their vehicle description, like "White 2000 Toyota 4Runner"
    /// </summary>
    public string VehicleDescription { get; set; } = "";

    /// <summary>
    /// A custom message they'd like displayed, in plain text.
    /// </summary>
    public string Message { get; set; } = "";
  }
}