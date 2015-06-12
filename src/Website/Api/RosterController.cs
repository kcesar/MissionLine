/*
 * Copyright 2015 Matt Cosand
 */
namespace Kcesar.MissionLine.Website.Api
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Linq.Expressions;
  using System.Web.Http;
  using Kcesar.MissionLine.Website.Api.Model;
  using Kcesar.MissionLine.Website.Data;

  /// <summary>
  /// 
  /// </summary>
  public class RosterController : ApiController
  {
    private readonly IConfigSource config;
    private readonly IMemberSource members;
    private readonly Func<IMissionLineDbContext> dbFactory;

    /// <summary>
    /// 
    /// </summary>
    public RosterController()
      : this(() => new MissionLineDbContext(), new ConfigSource(), new MemberSource(new ConfigSource()))
    {
    }

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

        var latest = (from s in db.SignIns
                      group s by s.MemberId into g
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

    internal static RosterEntry GetRosterEntry(int id, IMissionLineDbContext db)
    {
      return db.SignIns.Where(f => f.Id == id)
        .Select(proj)
        .SingleOrDefault();      
    }

    private static Expression<Func<MemberSignIn, RosterEntry>> proj = f => new RosterEntry
    {
      Id = f.Id,
      Name = f.Name,
      MemberId = f.MemberId,
      IsMember = f.isMember ? f.MemberId : null,
      TimeIn = f.TimeIn,
      TimeOut = f.TimeOut,
      State = f.TimeOut.HasValue ? RosterState.SignedOut : RosterState.SignedIn,
      Miles = f.Miles,
      EventId = f.EventId
    };
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