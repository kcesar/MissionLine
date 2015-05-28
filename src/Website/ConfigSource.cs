using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Kcesar.MissionLine.Website
{
  public interface IConfigSource
  {
    string GetConfig(string key);
  }

  public class ConfigSource : IConfigSource
  {
    public string GetConfig(string key)
    {
      return ConfigurationManager.AppSettings[key];
    }
  }
}