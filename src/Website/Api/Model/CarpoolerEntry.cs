using Kcesar.MissionLine.Website.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Kcesar.MissionLine.Website.Api.Model
{
  public class CarpoolerEntry
  {
    public CarpoolerEntry(Carpooler data)
    {
      CanBeDriver = data.CanBeDriver;
      CanBePassenger = data.CanBePassenger;
      LocationLatitude = data.LocationLatitude;
      LocationLongitude = data.LocationLongitude;
      VehicleDescription = data.VehicleDescription;
      Message = data.Message;
    }

    public CarpoolerEntry() { }

    public MemberLookupResult Member { get; set; }

    public bool? CanBeDriver { get; set; }

    public bool? CanBePassenger { get; set; }

    public decimal? LocationLatitude { get; set; }

    public decimal? LocationLongitude { get; set; }

    public string VehicleDescription { get; set; }

    public string Message { get; set; }

    /// <summary>
    /// TODO: Only initialized when viewing a specific contact? Maybe we could do this all via JS?
    /// </summary>
    public List<PersonContact> PersonContacts { get; set; }
  }
}