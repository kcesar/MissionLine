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

  /// <summary>
  /// 
  /// </summary>
  public class CallsController : ApiController
  {
    private readonly IConfigSource config;
    private readonly IMemberSource members;
    private readonly Func<IMissionLineDbContext> dbFactory;

    /// <summary>
    /// 
    /// </summary>
    public CallsController()
      : this(() => new MissionLineDbContext(), new ConfigSource(), MemberSource.Create(new ConfigSource()))
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbFactory"></param>
    /// <param name="config"></param>
    /// <param name="members"></param>
    public CallsController(Func<IMissionLineDbContext> dbFactory, IConfigSource config, IMemberSource members)
    {
      this.dbFactory = dbFactory;
      this.config = config;
      this.members = members;
    }

    // GET api/<controller>
    public async Task<IEnumerable<CallEntry>> Get()
    {
      DateTime cutoff = DateTimeOffset.Now.AddDays(-2).ToOrgTime(config).ToLocalTime();
      using (var db = this.dbFactory())
      {
        return await db.Calls.Where(f => f.CallTime > cutoff).OrderByDescending(f => f.CallTime).Select(proj).ToArrayAsync();
      }
    }

    // GET api/<controller>/5
    public async Task<CallEntry> Get(int id)
    {
      using (var db = this.dbFactory())
      {
        return await GetCallEntry(id, db);
      }
    }

    internal static CallEntry GetCallEntry(VoiceCall call)
    {
      return new[] { call }.AsQueryable().Select(proj).Single();
    }

    internal async static Task<CallEntry> GetCallEntry(int id, IMissionLineDbContext db)
    {
      return await db.Calls.Where(f => f.Id == id)
        .Select(proj)
        .SingleOrDefaultAsync();
    }

    private static Expression<Func<VoiceCall, CallEntry>> proj = f => new CallEntry
    {
      Id = f.Id,
      Number = f.Number,
      Name = f.Name,
      Time = f.CallTime,
      Recording = f.RecordingUrl
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