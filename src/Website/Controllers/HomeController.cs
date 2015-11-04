/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Controllers
{
  using System.Threading.Tasks;
  using System.Web.Mvc;
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
    public ActionResult Heartbeat()
    {
      log.Debug("Heartbeat");
      return Content("OK");
    }
  }
}