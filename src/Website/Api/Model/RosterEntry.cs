/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Api.Model
{
  using System;

  public class RosterEntry
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset TimeIn { get; set; }
    public DateTimeOffset? TimeOut { get; set; }
    public string MemberId { get; set; }
    public bool IsMember { get; set; }

    public int? EventId { get; set; }

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