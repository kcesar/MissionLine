/*
 * Copyright Matthew Cosand
 */
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
    IDbSet<Carpooler> Carpoolers { get; set; }

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
    public IDbSet<Carpooler> Carpoolers { get; set; }

    public IDbSet<LogEntry> Logs { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.HasDefaultSchema("missionline");
      modelBuilder.Entity<Carpooler>().Property(i => i.LocationLatitude).HasPrecision(18, 12);
      modelBuilder.Entity<Carpooler>().Property(i => i.LocationLongitude).HasPrecision(18, 12);
    }

    public static MissionLineDbContext Create()
    {
      return new MissionLineDbContext();
    }
  }
}