/*
 * Copyright 2015 Matt Cosand
 */
namespace Kcesar.MissionLine.Website.Api
{
  using System;
  using System.Collections.Generic;
  using System.Data.Entity;
  using System.Linq;
  using System.Linq.Expressions;
  using System.Threading.Tasks;
  using System.Web.Http;
  using Data;
  using Model;
  using Website.Model;

  /// <summary>
  /// 
  /// </summary>
  public class EventsController : ApiController
  {
    private readonly IConfigSource config;
    private readonly Func<IMissionLineDbContext> dbFactory;

    private static DateTime minDate = new DateTime(2000, 1, 1);
    private static DateTime maxDate = new DateTime(2100, 1, 1);

    /// <summary>
    /// 
    /// </summary>
    public EventsController()
      : this(() => new MissionLineDbContext(), new ConfigSource())
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbFactory"></param>
    /// <param name="config"></param>
    public EventsController(Func<IMissionLineDbContext> dbFactory, IConfigSource config)
    {
      this.dbFactory = dbFactory;
      this.config = config;
    }

    // GET api/<controller>
    public IEnumerable<EventEntry> Get()
    {
      using (var db = this.dbFactory())
      {
        return GetActiveEvents(db, config).Select(proj).ToArray();
      }
    }

    internal static IQueryable<SarEvent> GetActiveEvents(IMissionLineDbContext db, IConfigSource config)
    {
      IQueryable<SarEvent> query = db.Events;
      DateTime cutoff = DateTimeOffset.Now.AddDays(-2).ToOrgTime(config).ToLocalTime();
      query = query.Where(f => f.Closed == null || f.Closed > cutoff);
      return query.OrderByDescending(f => f.Opened);
    }


    // GET api/<controller>/5
    public EventEntry Get(int id)
    {
      using (var db = this.dbFactory())
      {
        return GetEventEntry(id, db);
      }
    }

    [HttpPost]
    [Route("api/events/{id}/close")]
    public async Task<SubmitResult> Close(int id)
    {
      var result = new SubmitResult();
      using (var db = dbFactory())
      {
        var rosterCount = await db.SignIns.Where(f => f.EventId == id && f.TimeOut == null).CountAsync();
        if (rosterCount > 0)
        {
          result.Errors.Add(new SubmitError("All members must be signed out before an event can be closed"));
        }

        if (result.Errors.Count == 0)
        {
          var e = await db.Events.SingleOrDefaultAsync(f => f.Id == id);
          e.Closed = DateTimeOffset.Now.ToOrgTime(this.config);
          await db.SaveChangesAsync();
          this.config.GetPushHub<CallsHub>().updatedEvent(compiledProj(e));
        }
      }
      return result;
    }

    [HttpPost]
    [Route("api/events/{id}/reopen")]
    public async Task<SubmitResult> Reopen(int id)
    {
      var result = new SubmitResult();
      using (var db = dbFactory())
      {
        var e = await db.Events.SingleOrDefaultAsync(f => f.Id == id);
        e.Closed = null;
        await db.SaveChangesAsync();
        this.config.GetPushHub<CallsHub>().updatedEvent(compiledProj(e));
      }
      return result;
    }

    [HttpPost]
    [Route("api/events/{fromId}/merge/{intoId}")]
    public async Task<SubmitResult<EventEntry>> Merge(int fromId, int intoId)
    {
      var result = new SubmitResult<EventEntry>();
      List<Action> notifications = new List<Action>();
      var hub = this.config.GetPushHub<CallsHub>();

      using (var db = dbFactory())
      {
        var from = await db.Events.Include(f => f.SignIns).SingleOrDefaultAsync(f => f.Id == fromId);
        var into = await db.Events.Include(f => f.SignIns).SingleOrDefaultAsync(f => f.Id == intoId);

        var fromSignins = from.SignIns.ToList();
        var intoSignins = into.SignIns.ToList();

        foreach (var call in from.Calls)
        {
          call.EventId = intoId;
        }

        var allSignins = from.SignIns.Concat(into.SignIns).OrderBy(f => f.MemberId).ThenBy(f => f.TimeIn).ToArray();
        MemberSignIn lastSignin = null;
        for (int i = 0; i < allSignins.Length; i++)
        {
          var thisSignin = allSignins[i];
          if (lastSignin != null && lastSignin.MemberId == thisSignin.MemberId)
          {
            if (lastSignin.TimeOut == null || thisSignin.TimeIn <= lastSignin.TimeOut.Value)
            {
              if (thisSignin.TimeOut == null || lastSignin.TimeOut == null || thisSignin.TimeOut > lastSignin.TimeOut)
              {
                lastSignin.TimeOut = thisSignin.TimeOut;
              }
              var milesSum = lastSignin.Miles ?? 0 + thisSignin.Miles ?? 0;
              lastSignin.Miles = (lastSignin.Miles.HasValue || thisSignin.Miles.HasValue) ? milesSum : (int?)null;
              if (thisSignin.EventId == intoId)
              {
                into.SignIns.Remove(thisSignin);
              }
              continue;
            }
          }
          if (thisSignin.EventId != intoId)
          {
            into.SignIns.Add(thisSignin);
          }
          lastSignin = thisSignin;
        }          

        if (string.IsNullOrWhiteSpace(into.OutgoingText))
        {
          into.OutgoingText = from.OutgoingText;
        }
        if (string.IsNullOrWhiteSpace(into.OutgoingUrl))
        {
          into.OutgoingUrl = from.OutgoingUrl;
        }
        if (string.IsNullOrWhiteSpace(into.DirectionsText))
        {
          into.DirectionsText = from.DirectionsText;
        }
        if (string.IsNullOrWhiteSpace(into.DirectionsUrl))
        {
          into.DirectionsUrl = from.DirectionsUrl;
        }
        if (into.Closed == null)
        {
          into.Closed = from.Closed;
        }

        db.Events.Remove(from);

        await db.SaveChangesAsync();
        result.Data = compiledProj(into);
        hub.removedEvent(from.Id);
        hub.updatedEvent(result.Data);
      }
      return new SubmitResult<EventEntry>();
    }

    internal static EventEntry GetEventEntry(SarEvent evt)
    {
      return compiledProj(evt);
    }

    internal static EventEntry GetEventEntry(int id, IMissionLineDbContext db)
    {
      return db.Events.Where(f => f.Id == id)
        .Select(proj)
        .SingleOrDefault();
    }

    private static Expression<Func<SarEvent, EventEntry>> proj = f => new EventEntry
    {
      Id = f.Id,
      Name = f.Name,
      Opened = f.Opened,
      Closed = f.Closed
    };
    private static Func<SarEvent, EventEntry> compiledProj = proj.Compile();
    
    // POST api/<controller>
    public async Task<SubmitResult<EventEntry>> Post(EventEntry value)
    {
      return await CreateEvent(value, dbFactory, config);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="dbFactory"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    internal static async Task<SubmitResult<EventEntry>> CreateEvent(EventEntry value, Func<IMissionLineDbContext> dbFactory, IConfigSource config)
    {
      var result = new SubmitResult<EventEntry>();
      if (string.IsNullOrWhiteSpace(value.Name))
      {
        result.Errors.Add(new SubmitError("name", "Required"));
      }
      DateTime localTime = value.Opened.ToOrgTime(config);
      if (localTime < minDate || localTime > maxDate)
      {
        result.Errors.Add(new SubmitError("opened", "Date invalid or out of range"));
      }

      if (result.Errors.Count == 0)
      {
        var evt = new SarEvent
        {
          Name = value.Name,
          Opened = localTime
        };
        using (var db = dbFactory())
        {
          db.Events.Add(evt);
          await db.SaveChangesAsync();
          result.Data = new[] { evt }.AsQueryable().Select(proj).Single();
          config.GetPushHub<CallsHub>().updatedEvent(result.Data);
        }
      }
      return result;
    }

    /*
    // PUT api/<controller>/5
    public void Put(int id, [FromBody]string value)
    {
    }

    // DELETE api/<controller>/5
    public void Delete(int id)
    {
    }
     * */
  }
}