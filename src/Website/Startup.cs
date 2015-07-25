/*
 * Copyright 2015 Matthew Cosand
 */
[assembly: Microsoft.Owin.OwinStartup(typeof(Kcesar.MissionLine.Website.Startup))]

namespace Kcesar.MissionLine.Website
{
  using System;
  using Microsoft.AspNet.SignalR;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;
  using Owin;

  public partial class Startup
  {
    private static readonly Lazy<JsonSerializer> JsonSerializerFactory = new Lazy<JsonSerializer>(GetJsonSerializer);
    private static JsonSerializer GetJsonSerializer()
    {
      var serializer = new JsonSerializer
      {
        ContractResolver = new FilteredCamelCasePropertyNamesContractResolver("Kcesar")
      };
      serializer.Converters.Add(new StringEnumConverter());
      return serializer;
    }

    public void Configuration(IAppBuilder app)
    {
      ConfigureAuth(app, new ConfigSource());
      app.MapSignalR();
      GlobalHost.DependencyResolver.Register(
        typeof(JsonSerializer),
        () => JsonSerializerFactory.Value);
    }
  }
}