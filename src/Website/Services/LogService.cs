/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Services
{
  using System;
  using System.IO;
  using log4net.Config;

  public class LogService
  {
    public static void ProcessSetup()
    {
      string thisFile = new Uri(typeof(LogService).Assembly.CodeBase).LocalPath;
      string configFile = Path.Combine(Path.GetDirectoryName(thisFile), "..", "log4net.config");
      if (File.Exists(configFile))
      {
        XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(configFile));
      }
    }
  }
}