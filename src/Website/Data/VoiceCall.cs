using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Kcesar.MissionLine.Website.Data
{
  public class VoiceCall
  {
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
  }
}