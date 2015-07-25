/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System.Configuration;
  using System.Web.Mvc;
  using Microsoft.AspNet.SignalR;

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