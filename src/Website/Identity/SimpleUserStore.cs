namespace Kcesar.MissionLine.Website.Identity
{
  using System;
  using System.Collections.Generic;
  using System.Data.Entity;
  using System.Linq;
  using System.Threading.Tasks;
  using Data;
  using Microsoft.AspNet.Identity;

  public class SimpleUserStore : IUserStore<ApplicationUser>,
    IUserLoginStore<ApplicationUser>,
    IUserLockoutStore<ApplicationUser, string>,
    IUserTwoFactorStore<ApplicationUser, string>
  {
    private MissionLineDbContext context;
    public SimpleUserStore(MissionLineDbContext context)
    {
      this.context = context;
    }

    public Task AddLoginAsync(ApplicationUser user, UserLoginInfo login)
    {
      user.Logins.Add(new UserLogin { LoginProvider = login.LoginProvider, ProviderKey = login.ProviderKey });
      return context.SaveChangesAsync();
    }

    public Task CreateAsync(ApplicationUser user)
    {
      context.Users.Add(user);
      user.Id = Guid.NewGuid().ToString();
      return context.SaveChangesAsync();
    }

    public Task DeleteAsync(ApplicationUser user)
    {
      throw new NotImplementedException();
    }

    public void Dispose()
    {
      if (this.context != null)
      {
        this.context.Dispose();
        this.context = null;
      }
    }

    public async Task<ApplicationUser> FindAsync(UserLoginInfo login)
    {
      return await context.UserLogins
        .Where(f => f.LoginProvider == login.LoginProvider && f.ProviderKey == login.ProviderKey)
        .Select(f => f.User)
        .SingleOrDefaultAsync();
    }

    public async Task<ApplicationUser> FindByIdAsync(string userId)
    {
      return await context.Users.SingleOrDefaultAsync(f => f.Id == userId);
    }

    public async Task<ApplicationUser> FindByNameAsync(string userName)
    {
      return await context.Users.SingleOrDefaultAsync(f => f.UserName == userName);
    }

    public Task<IList<UserLoginInfo>> GetLoginsAsync(ApplicationUser user)
    {
      throw new NotImplementedException();
    }

    public Task RemoveLoginAsync(ApplicationUser user, UserLoginInfo login)
    {
      throw new NotImplementedException();
    }

    public Task UpdateAsync(ApplicationUser user)
    {
      if (user != null)
      {
        context.Entry(user).State = EntityState.Modified;
      }
      return context.SaveChangesAsync();
    }

    Task<int> IUserLockoutStore<ApplicationUser, string>.GetAccessFailedCountAsync(ApplicationUser user)
    {
      throw new NotImplementedException();
    }

    Task<bool> IUserLockoutStore<ApplicationUser, string>.GetLockoutEnabledAsync(ApplicationUser user)
    {
      return Task.FromResult(false);
    }

    Task<DateTimeOffset> IUserLockoutStore<ApplicationUser, string>.GetLockoutEndDateAsync(ApplicationUser user)
    {
      throw new NotImplementedException();
    }

    Task<bool> IUserTwoFactorStore<ApplicationUser, string>.GetTwoFactorEnabledAsync(ApplicationUser user)
    {
      return Task.FromResult(false);
    }

    Task<int> IUserLockoutStore<ApplicationUser, string>.IncrementAccessFailedCountAsync(ApplicationUser user)
    {
      throw new NotImplementedException();
    }

    Task IUserLockoutStore<ApplicationUser, string>.ResetAccessFailedCountAsync(ApplicationUser user)
    {
      throw new NotImplementedException();
    }

    Task IUserLockoutStore<ApplicationUser, string>.SetLockoutEnabledAsync(ApplicationUser user, bool enabled)
    {
      throw new NotImplementedException();
    }

    Task IUserLockoutStore<ApplicationUser, string>.SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset lockoutEnd)
    {
      throw new NotImplementedException();
    }

    Task IUserTwoFactorStore<ApplicationUser, string>.SetTwoFactorEnabledAsync(ApplicationUser user, bool enabled)
    {
      throw new NotImplementedException();
    }
  }

}