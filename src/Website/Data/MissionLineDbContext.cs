using System;
using System.Data.Entity;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Kcesar.MissionLine.Website.Data
{
  public interface IMissionLineDbContext : IDisposable
  {
    IDbSet<VoiceCall> Calls { get; set; }
    IDbSet<MemberSignIn> SignIns { get; set; }
    IDbSet<SarEvent> Events { get; set; }

    int SaveChanges();
    Task<int> SaveChangesAsync();
  }

  public class MissionLineDbContext : IdentityDbContext<ApplicationUser>, IMissionLineDbContext
  {
    public MissionLineDbContext()
      : base("DefaultConnection")
    {
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.Types().Configure(e => e.ToTable("missionline_" + e.ClrType.Name));
      modelBuilder.Entity<ApplicationUser>().ToTable("missionline_AppUser");
      modelBuilder.Entity<IdentityUser>().ToTable("missionline_User");
      modelBuilder.Entity<IdentityRole>().ToTable("missionline_Role");
      modelBuilder.Entity<IdentityUserRole>().ToTable("missionline_UserRole");
      modelBuilder.Entity<IdentityUserLogin>().ToTable("missionline_UserLogin");
      modelBuilder.Entity<IdentityUserClaim>().ToTable("missionline_UserClaim");
    }

    public IDbSet<VoiceCall> Calls { get; set; }
    public IDbSet<MemberSignIn> SignIns { get; set; }
    public IDbSet<SarEvent> Events { get; set; }

    public static MissionLineDbContext Create()
    {
      return new MissionLineDbContext();
    }
  }
}