using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Kcesar.MissionLine.Website.Api.Model
{
  public class RosterEntry
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime TimeIn { get; set; }
    public DateTime? TimeOut { get; set; }

    public decimal? Hours
    {
      get
      {
        if (this.TimeOut == null) return null;
        return Math.Round((decimal)(this.TimeOut.Value - this.TimeIn).TotalHours * 4) / 4M;
      }
    }
    public int? Miles { get; set; }
    public RosterState State { get; set; }
  }
  
  public enum RosterState
  {
    SignedIn,
    SignedOut
  }
}