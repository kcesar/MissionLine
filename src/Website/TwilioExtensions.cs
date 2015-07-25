/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using Twilio.TwiML;

  public static class TwilioExtensions
  {
    public static void SayVoice(this TwilioResponse response, string format, params object[] args)
    {
      response.Say(string.Format(format, args), new { voice = "woman" });
    }
  }
}