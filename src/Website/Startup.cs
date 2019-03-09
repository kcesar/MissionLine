/*
 * Copyright 2015 Matthew Cosand
 */
[assembly: Microsoft.Owin.OwinStartup(typeof(Kcesar.MissionLine.Website.Startup))]

namespace Kcesar.MissionLine.Website
{
  using System;
  using System.Configuration;
  using System.Data.Entity;
  using System.Net;
  using System.Web.Http;
  using Microsoft.AspNet.SignalR;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;
  using Ninject;
  using Ninject.Web.Common.OwinHost;
  using Ninject.Web.WebApi.OwinHost;
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
      System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
      var config = new HttpConfiguration();

      WebApiConfig.Register(config);

      var kernel = DIConfig.CreateKernel.Value;

      if (ConfigurationManager.AppSettings["autoUpdateDatabase"] != null)
      {
        Database.SetInitializer(new MigrateDatabaseToLatestVersion<Data.MissionLineDbContext, Kcesar.MissionLine.Website.Migrations.Configuration>());
      }

      ConfigureAuth(app, kernel.Get<IConfigSource>());
      app.MapSignalR();

      app.UseNinjectMiddleware(() => kernel);
      app.UseNinjectWebApi(config);

      GlobalHost.DependencyResolver.Register(
        typeof(JsonSerializer),
        () => JsonSerializerFactory.Value);
    }
  }
}