using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Twilio.TwiML;

namespace Kcesar.MissionLine.Website
{
  public static class TwilioExtensions
  {
    public static void SayVoice(this TwilioResponse response, string format, params object[] args)
    {
      response.Say(string.Format(format, args), new { voice = "woman" });
    }
  }
}