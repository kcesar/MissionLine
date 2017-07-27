namespace Kcesar.MissionLine.Website
{
  using System;
  using System.Configuration;
  using System.Data.Entity;
  using System.Web.Mvc;
  using System.Web.Optimization;
  using System.Web.Routing;
  using log4net;
  using Services;

  public class MvcApplication : System.Web.HttpApplication
  {
    protected void Application_Start()
    {
      LogService.ProcessSetup();

      if (ConfigurationManager.AppSettings["autoUpdateDatabase"] != null)
      {
        Database.SetInitializer(new MigrateDatabaseToLatestVersion<Data.MissionLineDbContext, Migrations.Configuration>());
      }
      AreaRegistration.RegisterAllAreas();
      FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
      RouteConfig.RegisterRoutes(RouteTable.Routes);
      BundleConfig.RegisterBundles(BundleTable.Bundles);

      ControllerBuilder.Current.SetControllerFactory(new DIControllerFactory(DIConfig.CreateKernel.Value));
    }

    protected void Application_Error(Object sender, EventArgs e)
    {
      var raisedException = Server.GetLastError();
      LogManager.GetLogger("Application").Error("Unhandled error", raisedException);
    }

    // Redirect http requests to the https URL
    protected void Application_BeginRequest()
    {
      if (!Context.Request.IsSecureConnection && !Context.Request.Url.Host.StartsWith("localhost"))
      {
        // This is an insecure connection, so redirect to the secure version
        UriBuilder uri = new UriBuilder(Context.Request.Url)
        {
          Scheme = "https",
          Port = 443
        };
        Response.Redirect(uri.ToString());
      }
    }
  }
}
