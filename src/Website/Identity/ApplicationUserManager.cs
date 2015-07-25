/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Identity
{
  using System;
  using System.Data.Entity;
  using System.Threading.Tasks;
  using Data;
  using Microsoft.AspNet.Identity;
  using Microsoft.AspNet.Identity.Owin;
  using Microsoft.Owin;

  // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
  public class ApplicationUserManager : UserManager<ApplicationUser>
  {
    private readonly IMissionLineDbContext dbContext;

    public ApplicationUserManager(IMissionLineDbContext dbContext, IUserStore<ApplicationUser> store)
        : base(store)
    {
      this.dbContext = dbContext;
    }

    public async Task<ApplicationUser> FindByLinkCodeAsync(string code)
    {
      return await dbContext.Users.SingleOrDefaultAsync(f => f.LinkCode == code && f.LinkCodeExpires > DateTime.Now);
    }

    public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
    {
      var db = context.Get<MissionLineDbContext>();
      var manager = new ApplicationUserManager(db, new SimpleUserStore(db));
      return manager;
    }
  }
}