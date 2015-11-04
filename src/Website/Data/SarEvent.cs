/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Data
{
  using System;
  using System.Collections.Generic;

  public class SarEvent
  {
    public SarEvent()
    {
      this.SignIns = new List<MemberSignIn>();
      this.Calls = new List<VoiceCall>();
    }

    public int Id { get; set; }
    public string Name { get; set; }

    public DateTimeOffset Opened { get; set; }
    public DateTimeOffset? Closed { get; set; }

    public string OutgoingText { get; set; }
    public string OutgoingUrl { get; set; }

    public string DirectionsText { get; set; }
    public string DirectionsUrl { get; set; }

    public virtual ICollection<MemberSignIn> SignIns { get; set; }
    public virtual ICollection<VoiceCall> Calls { get; set; }
  }
}