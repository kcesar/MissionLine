using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;

namespace Kcesar.MissionLine.Website
{
  public interface IConfigSource
  {
    string GetConfig(string key);
    string GetUrlAction(UrlHelper url, string action);
    dynamic GetPushHub<T>() where T : Hub;
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
    
    public dynamic GetPushHub<T>() where T : Hub
    {
      return GlobalHost.ConnectionManager.GetHubContext<T>().Clients.All;
    }
  }
}