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

  /// </summary>
  public class RosterController : ApiController
  {
    private readonly IConfigSource config;
    private readonly IMemberSource members;
    private readonly Func<IMissionLineDbContext> dbFactory;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbFactory"></param>
    /// <param name="config"></param>
    /// <param name="members"></param>
    public RosterController(Func<IMissionLineDbContext> dbFactory, IConfigSource config, IMemberSource members)
    {
      this.dbFactory = dbFactory;
      this.config = config;
      this.members = members;
    }

    // GET api/<controller>
    public IEnumerable<RosterEntry> Get()
    {
      using (var db = this.dbFactory())
      {
        var futureDate = DateTime.Now.AddYears(1);
        var closedEventCutoff = DateTimeOffset.Now.AddDays(-2).ToOrgTime(config).ToLocalTime();

        var latest = (from s in db.SignIns where s.TimeOut == null || s.TimeOut > closedEventCutoff || (s.EventId.HasValue && s.Event.Closed == null)
                      group s by new { s.MemberId, s.EventId } into g
                      let f = g.OrderByDescending(x => x.TimeIn).FirstOrDefault()
                      orderby f.TimeOut.HasValue ? f.TimeOut : futureDate descending, f.TimeIn
                      select f)
                      .Select(proj);

        return latest.ToArray();
      }
    }

    // GET api/<controller>/5
    public RosterEntry Get(int id)
    {
      using (var db = this.dbFactory())
      {
        return GetRosterEntry(id, db);
      }
    }

    [HttpPost]
    [Route("api/roster/{rosterId}/reassign/{eventId}")]
    public async Task<SubmitResult> Assign(int rosterId, int eventId)
    {
      using (var db = dbFactory())
      {
        var signin = await db.SignIns.SingleOrDefaultAsync(f => f.Id == rosterId);
        if (signin == null)
        {
          var result = new SubmitResult();
          result.Errors.Add(new SubmitError("Roster entry not found"));
          return result;
        }
        return await AssignInternal(signin, eventId, db, this.config);
      }
    }

    internal static async Task<SubmitResult> AssignInternal(MemberSignIn signin, int? eventId, IMissionLineDbContext db, IConfigSource config)
    { 
      var result = new SubmitResult();
      var notifications = new List<Action>();
      var hub = config.GetPushHub<CallsHub>();

      var exposedSignin = await db.SignIns
        .Where(f => f.MemberId == signin.MemberId && f.EventId == signin.EventId && f.Id != signin.Id)
        .OrderByDescending(f => f.TimeIn)
        .FirstOrDefaultAsync();
      if (exposedSignin != null)
      {
        notifications.Add(() => hub.updatedRoster(compiledProj(exposedSignin), true));
      }

      var others = db.SignIns.Where(f => f.EventId == eventId && f.MemberId == signin.MemberId).OrderBy(f => f.TimeIn).ToList();
      DateTime effectiveTimeOut = signin.TimeOut ?? DateTime.MaxValue;
      bool rosterisLatest = true;

      foreach (var other in others)
      {
        var otherTimeOut = other.TimeOut ?? DateTime.MaxValue;
        // R: [---------->
        // O:      [----------]

        // R: [---------->
        // O:       [-------->

        // R: [----------]        
        // O:         [----->

        // R: [--------------------]
        // O:       [-------]

        // R: [---------]
        // O:     [--->

        // R: [------]
        // O:    [------]

        // R: [---]
        // O:       [----]

        // R:      [-----]
        // O:   [-----]

        // R:        [---]
        // O: [---]



        // Trim signins that overlap our time in.
        if (signin.TimeIn <= other.TimeIn)
        {
          if (effectiveTimeOut >= other.TimeIn && effectiveTimeOut <= otherTimeOut)
          {
            // roster is before the other one. It is not the latest
            rosterisLatest = false;
            notifications.Add(() => hub.updatedRoster(compiledProj(other), true));
            signin.TimeOut = other.TimeIn;
          }
          else if (effectiveTimeOut > otherTimeOut)
          {
            // split roster around other.
            // roster is the most recent, other and front are not.
            var front = SplitSignin(eventId, signin, other, db, config);
            notifications.Add(() => hub.updatedRoster(compiledProj(front), false));
            notifications.Add(() => hub.updatedRoster(compiledProj(other), false));
          }
        }
        else
        {
          if (otherTimeOut > signin.TimeIn && otherTimeOut <= effectiveTimeOut)
          {
            // other overlaps on the early side
            other.TimeOut = signin.TimeIn;
            notifications.Add(() => hub.updatedRoster(compiledProj(other), false));
          }
          else if (otherTimeOut > effectiveTimeOut)
          {
            //split other around roster
            var front = SplitSignin(eventId, other, signin, db, config);
            notifications.Add(() => hub.updatedRoster(compiledProj(front), false));
            notifications.Add(() => hub.updatedRoster(compiledProj(other), true));
            rosterisLatest = false;
          }
          else if (otherTimeOut < signin.TimeIn)
          {
            // other notification is not the latest.
            notifications.Add(() => hub.updatedRoster(compiledProj(other), false));
          }
        }
      }

      signin.EventId = eventId;
      notifications.Add(() => hub.updatedRoster(compiledProj(signin), rosterisLatest));
      db.SaveChanges();
      foreach (var notify in notifications)
      {
        notify();
      }

      return result;
    }

    private static MemberSignIn SplitSignin(int? eventId, MemberSignIn outer, MemberSignIn inner, IMissionLineDbContext db, IConfigSource config)
    {
      MemberSignIn front = new MemberSignIn
      {
        EventId = eventId,
        isMember = outer.isMember,
        MemberId = outer.MemberId,
        Name = outer.Name,
        TimeIn = outer.TimeIn,
        TimeOut = inner.TimeIn
      };
      db.SignIns.Add(front);
      outer.TimeIn = inner.TimeOut ?? DateTimeOffset.Now.ToOrgTime(config);
      return front;
    }

    [HttpPost]
    [Route("api/roster/{rosterId}/signout")]
    public SubmitResult Signout(int rosterId, DateTime when, int? miles)
    {
      var result = new SubmitResult();
      using (var db = dbFactory())
      {
        var roster = db.SignIns.SingleOrDefault(f => f.Id == rosterId);
        if (roster == null)
        {
          result.Errors.Add(new SubmitError("Roster entry not found"));
        }
        else
        {
          roster.TimeOut = when;
          roster.Miles = miles;
          db.SaveChanges();
          this.config.GetPushHub<CallsHub>().updatedRoster(compiledProj(roster));
        }
      }
      return result;
    }

    [HttpPost]
    [Route("api/roster/{rosterId}/undoSignout")]
    public SubmitResult UndoSignout(int rosterId)
    {
      var result = new SubmitResult();
      using (var db = dbFactory())
      {
        var roster = db.SignIns.SingleOrDefault(f => f.Id == rosterId);
        if (roster == null)
        {
          result.Errors.Add(new SubmitError("Roster entry not found"));
        }
        else
        {
          roster.TimeOut = null;
          db.SaveChanges();
          this.config.GetPushHub<CallsHub>().updatedRoster(compiledProj(roster));
        }
      }
      return result;
    }

    internal static RosterEntry GetRosterEntry(int id, IMissionLineDbContext db)
    {
      return db.SignIns.Where(f => f.Id == id)
        .Select(proj)
        .SingleOrDefault();
    }

    public async Task<SubmitResult<RosterEntry>> Put(MemberSignIn value)
    {
      var result = new SubmitResult<RosterEntry>();

      if (result.Errors.Count == 0)
      {
        using (var db = dbFactory())
        {
          MemberSignIn signin;
          signin = db.SignIns.Single(f => f.Id == value.Id);

          if (signin.TimeOut != value.TimeOut) { signin.TimeOut = value.TimeOut; }
          if (signin.Miles != value.Miles) { signin.Miles = value.Miles; }

          await db.SaveChangesAsync();
          result.Data = new[] { signin }.AsQueryable().Select(proj).Single();
          this.config.GetPushHub<CallsHub>().updatedRoster(result.Data);
        }
      }
      return result;
    }

    private static Expression<Func<MemberSignIn, RosterEntry>> proj = f => new RosterEntry
    {
      Id = f.Id,
      Name = f.Name,
      MemberId = f.MemberId,
      IsMember = f.isMember,
      TimeIn = f.TimeIn,
      TimeOut = f.TimeOut,
      State = f.TimeOut.HasValue ? RosterState.SignedOut : RosterState.SignedIn,
      Miles = f.Miles,
      EventId = f.EventId
    };
    private static Func<MemberSignIn, RosterEntry> compiledProj = proj.Compile();
    /*
    // POST api/<controller>
    public void Post([FromBody]string value)
    {
    }

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