/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System.Configuration;
  using System.Data.Entity;
  using System.Web.Mvc;
  using System.Web.Optimization;
  using System.Web.Routing;
  using Services;

  public class MvcApplication : System.Web.HttpApplication
  {
    protected void Application_Start()
    {
      LogService.ProcessSetup();

      if (ConfigurationManager.AppSettings["autoUpdateDatabase"] != null)
      {
        Database.SetInitializer(new MigrateDatabaseToLatestVersion<Data.MissionLineDbContext, Kcesar.MissionLine.Website.Migrations.Configuration>());
      }
      AreaRegistration.RegisterAllAreas();
      FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
      RouteConfig.RegisterRoutes(RouteTable.Routes);
      BundleConfig.RegisterBundles(BundleTable.Bundles);

      ControllerBuilder.Current.SetControllerFactory(new DIControllerFactory(DIConfig.CreateKernel.Value));
    }
  }
}
