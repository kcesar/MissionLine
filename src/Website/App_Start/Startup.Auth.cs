/*
 * Copyright Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System.IdentityModel.Tokens;
  using System.Security.Claims;
  using Data;
  using Microsoft.AspNet.Identity;
  using Microsoft.Owin.Security.Cookies;
  using Microsoft.Owin.Security.OpenIdConnect;
  using Owin;

  public partial class Startup
  {
    // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
    public void ConfigureAuth(IAppBuilder app, IConfigSource config)
    {
      // Configure the db context, user manager and signin manager to use a single instance per request
      app.CreatePerOwinContext(MissionLineDbContext.Create);

      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
        AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie
      });
      app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
      {
        Authority = config.GetConfig("auth:authority").Trim('/') + "/",
        ClientId = config.GetConfig("auth:clientId"),
        RedirectUri = config.GetConfig("auth:redirect").Trim('/') + "/",
        ResponseType = "id_token",
        Scope = "openid email profile kcsara-profile",
        TokenValidationParameters = new TokenValidationParameters
        {
          NameClaimType = ClaimsIdentity.DefaultNameClaimType
        },
        SignInAsAuthenticationType = DefaultAuthenticationTypes.ApplicationCookie
      });
    }
  }
}