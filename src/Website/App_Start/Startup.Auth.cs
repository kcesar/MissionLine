/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System;
  using System.Web;
  using Data;
  using Identity;
  using Microsoft.AspNet.Identity;
  using Microsoft.Owin;
  using Microsoft.Owin.Security;
  using Microsoft.Owin.Security.Cookies;
  using Microsoft.Owin.Security.Google;
  using Microsoft.Owin.Security.OpenIdConnect;
  using Owin;

  public partial class Startup
  {
    // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
    public void ConfigureAuth(IAppBuilder app, IConfigSource config)
    {
      // Configure the db context, user manager and signin manager to use a single instance per request
      app.CreatePerOwinContext(MissionLineDbContext.Create);
      app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
      app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

      app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
        AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
        LoginPath = new PathString("/Account/Login"),
        Provider = new CookieAuthenticationProvider
        {
          OnApplyRedirect = ctx =>
          {
            if (!IsApiRequest(ctx.Request))
            {
              ctx.Response.Redirect(ctx.RedirectUri);
            }
          }
        }
      });

      app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

      // Uncomment the following lines to enable logging in with third party login providers
      //app.UseMicrosoftAccountAuthentication(
      //    clientId: "",
      //    clientSecret: "");

      //app.UseTwitterAuthentication(
      //   consumerKey: "",
      //   consumerSecret: "");

      if (!string.IsNullOrWhiteSpace(config.GetConfig("O365:ClientId")) && !string.IsNullOrWhiteSpace(config.GetConfig("O365:Authority")))
      {
        app.UseOpenIdConnectAuthentication(
                  new OpenIdConnectAuthenticationOptions
                  {
                    ClientId = config.GetConfig("O365:ClientId"),
                    Authority = config.GetConfig("O365:Authority"),
                    AuthenticationMode = AuthenticationMode.Passive,
                    AuthenticationType = "ESAR Office 365",
                    Caption = "ESAR Office 365"
                  });
      }

      if (!string.IsNullOrWhiteSpace(config.GetConfig("Google:ClientId")) && !string.IsNullOrWhiteSpace(config.GetConfig("Google:ClientSecret")))
      {
        app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
        {
          ClientId = config.GetConfig("Google:ClientId"),
          ClientSecret = config.GetConfig("Google:ClientSecret")
        });
      }

      if (!string.IsNullOrWhiteSpace(config.GetConfig("Facebook:AppId")) && !string.IsNullOrWhiteSpace(config.GetConfig("Facebook:AppSecret")))
      {
        app.UseFacebookAuthentication(
         appId: config.GetConfig("Facebook:AppId"),
         appSecret: config.GetConfig("Facebook:AppSecret"));
      }
    }

    private bool IsApiRequest(IOwinRequest request)
    {
      string apiPath = VirtualPathUtility.ToAbsolute("~/api");
      return request.Uri.LocalPath.StartsWith(apiPath, StringComparison.OrdinalIgnoreCase);
    }
  }
}