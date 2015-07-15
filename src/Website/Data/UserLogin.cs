namespace Kcesar.MissionLine.Website.Data
{
  using System.ComponentModel.DataAnnotations.Schema;
  using Microsoft.AspNet.Identity.EntityFramework;

  public class UserLogin : IdentityUserLogin
  {
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; }
  }
}