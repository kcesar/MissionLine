using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IdentityModel.Services;
using System.IdentityModel.Services.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Kcesar.MissionLine.Website
{
  public class MvcApplication : System.Web.HttpApplication
  {
    protected void Application_Start()
    {
      if (ConfigurationManager.AppSettings["autoUpdateDatabase"] != null)
      {
        Database.SetInitializer(new MigrateDatabaseToLatestVersion<Data.MissionLineDbContext, Kcesar.MissionLine.Website.Migrations.Configuration>());
      }
      FederatedAuthentication.FederationConfigurationCreated += FederatedAuthentication_FederationConfigurationCreated;
      AreaRegistration.RegisterAllAreas();
      IdentityConfig.ConfigureIdentity();
      GlobalConfiguration.Configure(WebApiConfig.Register);
      FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
      RouteConfig.RegisterRoutes(RouteTable.Routes);
      BundleConfig.RegisterBundles(BundleTable.Bundles);
    }

    void FederatedAuthentication_FederationConfigurationCreated(object sender, FederationConfigurationCreatedEventArgs e)
    {
      if (e.FederationConfiguration.CookieHandler != null)
      {
        e.FederationConfiguration.CookieHandler.RequireSsl = true;
      }
      e.FederationConfiguration.WsFederationConfiguration.PassiveRedirectEnabled = true;
      e.FederationConfiguration.WsFederationConfiguration.Issuer = ConfigurationManager.AppSettings["ida:Issuer"];
      string realm = ConfigurationManager.AppSettings["ida:Realm"];
      e.FederationConfiguration.WsFederationConfiguration.Realm = realm;;
      e.FederationConfiguration.WsFederationConfiguration.RequireHttps = true;
      e.FederationConfiguration.IdentityConfiguration.AudienceRestriction.AllowedAudienceUris.Add(new Uri(realm));
    }

    private void WSFederationAuthenticationModule_RedirectingToIdentityProvider(object sender, RedirectingToIdentityProviderEventArgs e)
    {
      if (!String.IsNullOrEmpty(IdentityConfig.Realm))
      {
        e.SignInRequestMessage.Realm = IdentityConfig.Realm;
      }
    }
  }
}
