﻿/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System;

  public static class TimeUtils
  {
    public static DateTime GetLocalDateTime(IConfigSource config)
    {
      return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, config.GetConfig("timezone") ?? "Pacific Standard Time");
    }
    public static string GetMiltaryTimeVoiceText(DateTime time)
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