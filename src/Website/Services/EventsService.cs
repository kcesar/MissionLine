using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Kcesar.MissionLine.Website.Data;
using System.Data.Entity;

namespace Kcesar.MissionLine.Website.Services
{
  public class EventsService : IEventsService
  {
    private readonly Func<IMissionLineDbContext> dbFactory;
    private readonly IConfigSource config;

    public EventsService(Func<IMissionLineDbContext> dbFactory, IConfigSource config)
    {
      this.dbFactory = dbFactory;
      this.config = config;
    }

    public async Task<List<SarEvent>> ListActive()
    {
      using (var db = dbFactory())
      {
        return await db.Events.Where(f => f.Closed == null).OrderByDescending(f => f.Opened).ToListAsync();
      }
    }

    public async Task<SarEvent> QuickClose(int id)
    {
      using (var db = dbFactory())
      {
        var target = await db.Events.SingleAsync(f => f.Id == id);
        target.Closed = TimeUtils.GetLocalDateTime(this.config);
        await db.SaveChangesAsync();
        // TODO: push notification
        return target;
      }
    }

    public async Task Create(SarEvent newEvent)
    {
      using (var db = dbFactory())
      {
        db.Events.Add(newEvent);
        await db.SaveChangesAsync();
        // TODO: push notification
      }
    }

    public async Task<SarEvent> SetRecordedDescription(int eventId, string url)
    {
      using (var db = dbFactory())
      {
        var target = await db.Events.SingleAsync(f => f.Id == eventId);
        target.OutgoingUrl = url;
        await db.SaveChangesAsync();
        // TODO: push notification
        return target;
      }
    }
  }

  public interface IEventsService
  {
    Task<List<SarEvent>> ListActive();
    Task<SarEvent> QuickClose(int id);
    Task Create(SarEvent newEvent);
    Task<SarEvent> SetRecordedDescription(int eventId, string url);
  }
}