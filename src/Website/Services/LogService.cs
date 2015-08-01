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
      if (File.Exists(thisFile))
      {
        System.IO.FileInfo fi = new System.IO.FileInfo(Path.Combine(Path.GetDirectoryName(thisFile), "..", "log4net.config"));
        XmlConfigurator.ConfigureAndWatch(fi);
      }
    }
  }
}