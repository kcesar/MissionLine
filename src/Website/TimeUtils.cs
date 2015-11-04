/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System;

  public static class TimeUtils
  {
    public static DateTimeOffset GetLocalDateTime(IConfigSource config)
    {
      return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, config.GetConfig("timezone") ?? "Pacific Standard Time");
    }

    public static string GetMiltaryTimeVoiceText(DateTimeOffset time)
    {
      // "m" is giving the same result as "M" (month + day)
      string minuteText = time.ToString("mm").TrimStart('0');
      if (time.Minute == 0)
      {
        minuteText = "hundred ";
      }
      else if (time.Minute < 10)
      {
        minuteText = "oh " + minuteText;
      }
      return time.ToString("H ") + minuteText;
    }
  }
}