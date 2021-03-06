﻿/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System.Web.Http;
  using System.Web.Http.ExceptionHandling;
  using Api;
  using Newtonsoft.Json.Converters;
  using Newtonsoft.Json.Serialization;

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

      config.Filters.Add(new AuthorizeAttribute());

      config.Services.Add(typeof(IExceptionLogger), new Log4NetExceptionLogger());

      var jsonSettings = config.Formatters.JsonFormatter.SerializerSettings;
      jsonSettings.Converters.Add(new StringEnumConverter());
      jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    }
  }
}
