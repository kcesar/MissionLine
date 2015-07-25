/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Data
{
  using System;
  using System.Collections.Generic;
  using System.Security.Claims;
  using System.Threading.Tasks;
  using Microsoft.AspNet.Identity;

  public class ApplicationUser : IUser
  {
    public ApplicationUser()
    {
      this.Logins = new List<UserLogin>();
    }

    public string Id { get; set; }

    public string UserName { get; set; }

    public string LinkCode { get; set; }

    public DateTime? LinkCodeExpires { get; set; }

    public virtual ICollection<UserLogin> Logins { get; set; }

    public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
    {
      // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
      var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
      // Add custom user claims here
      return userIdentity;
    }
  }
}