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

    public DbSet<IssuingAuthorityKey> IssuingAuthorityKeys { get; set; }

    public DbSet<Tenant> Tenants { get; set; }

    public IDbSet<VoiceCall> Calls { get; set; }
    public IDbSet<MemberSignIn> SignIns { get; set; }
    public IDbSet<SarEvent> Events { get; set; }
  }
}