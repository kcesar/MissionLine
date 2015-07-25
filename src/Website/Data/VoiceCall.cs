/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Data
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel.DataAnnotations.Schema;

  public class VoiceCall
  {
    public VoiceCall()
    {
      this.Actions = new List<CallAction>();
    }

    public int Id { get; set; }
    public string CallId { get; set; }
    public string Number { get; set; }
    public DateTime CallTime { get; set; }
    public string Name { get; set; }
    public int? Duration { get; set; }
    public string RecordingUrl { get; set; }
    public int? RecordingDuration { get; set; }
    public string Comments { get; set; }

    [ForeignKey("Event")]
    public int? EventId { get; set; }
    public virtual SarEvent Event { get; set; }

    public virtual ICollection<CallAction> Actions { get; set; }
  }
}