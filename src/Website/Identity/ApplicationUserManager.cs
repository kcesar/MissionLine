namespace Kcesar.MissionLine.Website.Identity
{
  using Data;
  using Microsoft.AspNet.Identity;
  using Microsoft.AspNet.Identity.Owin;
  using Microsoft.Owin;

  // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
  public class ApplicationUserManager : UserManager<ApplicationUser>
  {
    public ApplicationUserManager(IUserStore<ApplicationUser> store)
        : base(store)
    {
    }

    public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
    {
      var manager = new ApplicationUserManager(new SimpleUserStore(context.Get<MissionLineDbContext>()));
      return manager;
    }
  }
}