using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Kcesar.MissionLine.Website
{
  public static class WebApiConfig
  {
    public static void Register(HttpConfiguration config)
    {
      // Web API configuration and services

      // Web API routes
      config.MapHttpAttributeRoutes();

      config.Routes.MapHttpRoute(
          name: "DefaultApi",
          routeTemplate: "api/{controller}/{action}/{id}",
          defaults: new { action = RouteParameter.Optional, id = RouteParameter.Optional }
      );

      var jsonSettings = config.Formatters.JsonFormatter.SerializerSettings;
      jsonSettings.Converters.Add(new StringEnumConverter());
      jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    }
  }
}
