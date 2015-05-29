using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Kcesar.MissionLine.Website.Api.Model;
using Kcesar.MissionLine.Website.Data;

namespace Kcesar.MissionLine.Website.Api
{
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
                      select new RosterEntry
                      {
                        Id = f.Id,
                        Name = f.Name,
                        TimeIn = f.TimeIn,
                        TimeOut = f.TimeOut,
                        State = f.TimeOut.HasValue ? RosterState.SignedOut : RosterState.SignedIn,
                        Miles = f.Miles
                      });

        return latest.ToArray();
      }
    }

    // GET api/<controller>/5
    public string Get(int id)
    {
      return "value";
    }

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
  }
}