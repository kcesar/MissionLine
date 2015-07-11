using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace Kcesar.MissionLine.Website.Data
{
  public interface IMissionLineDbContext : IDisposable
  {
    IDbSet<IssuingAuthorityKey> IssuingAuthorityKeys { get; set; }

    IDbSet<Tenant> Tenants { get; set; }

    IDbSet<VoiceCall> Calls { get; set; }
    IDbSet<MemberSignIn> SignIns { get; set; }
    IDbSet<SarEvent> Events { get; set; }

    int SaveChanges();
    Task<int> SaveChangesAsync();
  }

  public class MissionLineDbContext : DbContext, IMissionLineDbContext
  {
    public MissionLineDbContext()
      : base("DefaultConnection")
    {
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      modelBuilder.Types().Configure(e => e.ToTable("missionline_" + e.ClrType.Name));
      base.OnModelCreating(modelBuilder);
    }

    public IDbSet<IssuingAuthorityKey> IssuingAuthorityKeys { get; set; }

    public IDbSet<Tenant> Tenants { get; set; }

    public IDbSet<VoiceCall> Calls { get; set; }
    public IDbSet<MemberSignIn> SignIns { get; set; }
    public IDbSet<SarEvent> Events { get; set; }
  }
}