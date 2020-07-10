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
  using System.Security.Claims;
  using System.Threading.Tasks;
  using System.Web.Http;
  using Data;
  using log4net;
  using Model;
  using Website.Model;

  /// <summary>

  /// </summary>
  public class RosterController : ApiController
  {
    private readonly IConfigSource config;
    private readonly IMemberSource members;
    private readonly Func<IMissionLineDbContext> dbFactory;
    private readonly ILog log;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbFactory"></param>
    /// <param name="config"></param>
    /// <param name="members"></param>
    /// <param name="log"></param>
    public RosterController(Func<IMissionLineDbContext> dbFactory, IConfigSource config, IMemberSource members, ILog log)
    {
      this.dbFactory = dbFactory;
      this.config = config;
      this.members = members;
      this.log = log;
    }

    // GET api/<controller>
    public IEnumerable<RosterEntry> Get()
    {
      using (var db = this.dbFactory())
      {
        var futureDate = DateTimeOffset.UtcNow.ToOrgTime(config).AddYears(1);
        var closedEventCutoff = DateTimeOffset.UtcNow.ToOrgTime(config).AddDays(-2);

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

    [HttpGet]
    [Route("api/roster/member/{memberId}")]
    public RosterEntry[] GetForMember(string memberId)
    {
      using (var db = this.dbFactory())
      {
        var futureDate = DateTimeOffset.UtcNow.ToOrgTime(config).AddYears(1);
        var closedEventCutoff = DateTimeOffset.UtcNow.ToOrgTime(config).AddDays(-2);

        var latest = (from s in db.SignIns
                      where (s.TimeOut == null || s.TimeOut > closedEventCutoff) && s.MemberId == memberId
                      orderby s.TimeOut.HasValue ? s.TimeOut : futureDate descending, s.TimeIn
                      select s)
                      .Select(proj);

        return latest.ToArray();
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

      var others = db.SignIns.Where(f => f.EventId == eventId && f.MemberId == signin.MemberId && f.Id != signin.Id).OrderBy(f => f.TimeIn).ToList();
      DateTimeOffset effectiveTimeOut = signin.TimeOut ?? DateTimeOffset.MaxValue;
      bool rosterisLatest = true;

      foreach (var other in others)
      {
        var otherTimeOut = other.TimeOut ?? DateTimeOffset.MaxValue;
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
      if (signin.Event == null && eventId.HasValue)
      {
        signin.Event = await db.Events.SingleAsync(f => f.Id == eventId);
      }
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
      outer.TimeIn = inner.TimeOut ?? DateTimeOffset.UtcNow.ToOrgTime(config);
      return front;
    }

    //[HttpPost]
    //[Route("api/roster/{rosterId}/signout")]
    //public async Task<SubmitResult> Signout(int rosterId, DateTime when, int? miles)
    //{
    //  var result = new SubmitResult();
    //  using (var db = dbFactory())
    //  {
    //    var roster = db.SignIns.SingleOrDefault(f => f.Id == rosterId);
    //    if (roster == null)
    //    {
    //      result.Errors.Add(new SubmitError("Roster entry not found"));
    //    }
    //    else
    //    {
    //      roster.TimeOut = when;
    //      roster.Miles = miles;
    //      await AssignInternal(roster, roster.EventId, db, config);
    //    }
    //  }
    //  return result;
    //}

    //[HttpPost]
    //[Route("api/roster/{rosterId}/undoSignout")]
    //public async Task<SubmitResult> UndoSignout(int rosterId)
    //{
    //  var result = new SubmitResult();
    //  using (var db = dbFactory())
    //  {
    //    var roster = db.SignIns.SingleOrDefault(f => f.Id == rosterId);
    //    if (roster == null)
    //    {
    //      result.Errors.Add(new SubmitError("Roster entry not found"));
    //    }
    //    else
    //    {
    //      roster.TimeOut = null;
    //      await AssignInternal(roster, roster.EventId, db, config);
    //    }
    //  }
    //  return result;
    //}

    internal static RosterEntry GetRosterEntry(int id, IMissionLineDbContext db)
    {
      return db.SignIns.Where(f => f.Id == id)
        .Select(proj)
        .SingleOrDefault();
    }

    public async Task<SubmitResult<RosterEntry>> Post(MemberSignIn value)
    {
      var result = new SubmitResult<RosterEntry>();
      log.InfoFormat("User {0} adding signin for member {1} on event {2}", User.Identity.Name, value.MemberId, value.EventId);

      var memberIdClaim = ((ClaimsPrincipal)User).FindFirst("memberId");
      if (memberIdClaim == null || memberIdClaim.Value != value.MemberId)
      {
        result.Errors.Add(new SubmitError("memberId", "Currently only supports signing in yourself."));
      }
      else
      {
        var nameClaim = ((ClaimsPrincipal)User).FindFirst("name");
        value.Name = nameClaim.Value;
        value.isMember = true;
      }

      return await SaveSigninInternal(value, result);
    }


    public async Task<SubmitResult<RosterEntry>> Put(MemberSignIn value)
    {
      log.InfoFormat("User {0} updating signin for member {1} ({2}) on event {3}. New timeout = {4}",
        User.Identity.Name,
        value.MemberId,
        value.Name,
        value.EventId,
        value.TimeOut);
      return await SaveSigninInternal(value, new SubmitResult<RosterEntry>());
    }

    private async Task<SubmitResult<RosterEntry>> SaveSigninInternal(MemberSignIn value, SubmitResult<RosterEntry> result)
    {
      if (result.Errors.Count == 0)
      {
        using (var db = dbFactory())
        {
          MemberSignIn signin;
          if (value.Id == 0)
          {
            signin = value;
            db.SignIns.Add(value);
          }
          else
          {
            signin = db.SignIns.Single(f => f.Id == value.Id);
          }

          if (signin.TimeOut != value.TimeOut) { signin.TimeOut = value.TimeOut; }
          if (signin.Miles != value.Miles) { signin.Miles = value.Miles; }
          await AssignInternal(signin, signin.EventId, db, config);
          result.Data = new[] { signin }.AsQueryable().Select(proj).Single();
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
      EventId = f.EventId,
      EventName = f.Event.Name
    };
    private static Func<MemberSignIn, RosterEntry> compiledProj = proj.Compile();
  }
}