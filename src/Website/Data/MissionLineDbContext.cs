namespace Kcesar.MissionLine.Website.Data
{
  using System;
  using System.Data.Entity;
  using System.Threading.Tasks;

  public interface IMissionLineDbContext : IDisposable
  {
    IDbSet<VoiceCall> Calls { get; set; }
    IDbSet<MemberSignIn> SignIns { get; set; }
    IDbSet<SarEvent> Events { get; set; }

    IDbSet<ApplicationUser> Users { get; set; }
    IDbSet<UserLogin> UserLogins { get; set; }
    int SaveChanges();
    Task<int> SaveChangesAsync();
  }

  public class MissionLineDbContext : DbContext, IMissionLineDbContext
  {
    public MissionLineDbContext()
      : base("DefaultConnection")
    {
    }

    public IDbSet<VoiceCall> Calls { get; set; }
    public IDbSet<MemberSignIn> SignIns { get; set; }
    public IDbSet<SarEvent> Events { get; set; }

    public IDbSet<ApplicationUser> Users { get; set; }
    public IDbSet<UserLogin> UserLogins { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.Types().Configure(e => e.ToTable("missionline_" + e.ClrType.Name));
      modelBuilder.Entity<ApplicationUser>().HasKey(f => f.Id).HasMany(f => f.Logins).WithRequired(f => f.User);
      modelBuilder.Entity<UserLogin>().HasKey(l => new { l.LoginProvider, l.ProviderKey, l.UserId });
    }

    public static MissionLineDbContext Create()
    {
      return new MissionLineDbContext();
    }
  }
}