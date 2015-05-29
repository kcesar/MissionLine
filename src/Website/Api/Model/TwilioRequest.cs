using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Kcesar.MissionLine.Website.Api.Model
{
  public class TwilioRequest
  {
    public string CallSid { get; set; }
    public string Digits { get; set; }
    public string From { get; set; }
    public int? CallDuration { get; set; }
    public int? RecordingDuration { get; set; }
    public string RecordingUrl { get; set; }
  }
}