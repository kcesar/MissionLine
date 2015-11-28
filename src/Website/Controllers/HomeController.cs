/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Controllers
{
  using System.Data.Entity;
  using System.Threading.Tasks;
  using System.Web.Mvc;
  using Data;
  using log4net;
  using Newtonsoft.Json;

  [RequireHttps]
  public class HomeController : Controller
  {
    private readonly IMemberSource members;
    private readonly IConfigSource config;
    private readonly ILog log;

    public HomeController(IMemberSource members, IConfigSource config, ILog log)
    {
      this.members = members;
      this.config = config;
      this.log = log;
    }

    public async Task<ActionResult> Index()
    {
      ViewBag.NgApp = "missionlineApp";
      ViewBag.LinkTemplate = this.config.GetConfig("memberLinkTemplate");
      ViewBag.Myself = JsonConvert.SerializeObject(await members.LookupMemberUsername(User.Identity.Name.Split('@')[0]));
      return View();
    }

    [AllowAnonymous]
    public async Task<ActionResult> Heartbeat()
    {
      using (var db = new MissionLineDbContext())
      {
        var info = await members.LookupMemberUsername("heartbeat");
        log.DebugFormat("Heartbeat {0} {1}", await db.Events.CountAsync(), info);
      }
      return Content("OK");
    }
  }
}