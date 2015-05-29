using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Kcesar.MissionLine.Website
{
  public interface IConfigSource
  {
    string GetConfig(string key);
    string GetUrlAction(UrlHelper url, string action);
  }

  public class ConfigSource : IConfigSource
  {
    public string GetConfig(string key)
    {
      return ConfigurationManager.AppSettings[key];
    }
    
    public string GetUrlAction(UrlHelper url, string action)
    {
      return url.Action(action);
    }
  }
}