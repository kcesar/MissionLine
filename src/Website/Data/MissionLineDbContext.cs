using System;
using System.Data.Entity;

namespace Kcesar.MissionLine.Website.Data
{
  public class MissionLineDbContext : DbContext
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

    public DbSet<IssuingAuthorityKey> IssuingAuthorityKeys { get; set; }

    public DbSet<Tenant> Tenants { get; set; }

    public IDbSet<VoiceCall> Calls { get; set; }
    public IDbSet<MemberSignIn> SignIns { get; set; }
    public IDbSet<SarEvent> Events { get; set; }
  }
}