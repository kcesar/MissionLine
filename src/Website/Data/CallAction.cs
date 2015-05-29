using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Kcesar.MissionLine.Website.Data
{
  public class CallAction
  {
    public int Id { get; set; }

    [ForeignKey("Call")]
    public int CallId { get; set; }
    public virtual VoiceCall Call { get; set; }

    public DateTime Time { get; set; }
    public string Action { get; set; }
  }
}