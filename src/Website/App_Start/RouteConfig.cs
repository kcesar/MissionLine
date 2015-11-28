/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System.Web.Mvc;
  using System.Web.Routing;

  public class RouteConfig
  {
    public static void RegisterRoutes(RouteCollection routes)
    {
      routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

      routes.MapRoute(
        name: "Google API Sign-in",
        url: "signin-google",
        defaults: new { controller = "Account", action = "ExternalLoginCallbackRedirect" }
      );

      routes.MapRoute(
        name: "Me",
        url: "Me",
        defaults: new { controller = "Home", action = "Me" }
        );

      routes.MapRoute(
          name: "Default",
          url: "{controller}/{action}/{id}",
          defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
      );
    }
  }
}
