/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Identity
{
  using System.Security.Claims;
  using System.Threading.Tasks;
  using Data;
  using Microsoft.AspNet.Identity.Owin;
  using Microsoft.Owin;
  using Microsoft.Owin.Security;

  // Configure the application sign-in manager which is used in this application.
  public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
  {
    public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
        : base(userManager, authenticationManager)
    {
    }

    public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
    {
      return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
    }

    public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
    {
      return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
    }

    public override Task SignInAsync(ApplicationUser user, bool isPersistent, bool rememberBrowser)
    {
      return base.SignInAsync(user, isPersistent, rememberBrowser);
    }
  }
}
