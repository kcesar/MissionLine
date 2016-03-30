/*
 * Copyright Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Controllers
{
  using System.Data.Entity;
  using System.Security.Claims;
  using System.Threading.Tasks;
  using System.Web.Mvc;
  using Data;
  using log4net;

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

    private MemberLookupResult GetMySelf()
    {
      var identity = (ClaimsIdentity)User.Identity;
      var memberIdClaim = identity.FindFirst("memberId");
      return new MemberLookupResult { Id = memberIdClaim.Value, Name = identity.FindFirst("name").Value };
    }

    public ActionResult Index()
    {
      ViewBag.LinkTemplate = config.GetConfig("memberLinkTemplate");
      ViewBag.Myself = GetMySelf();
      return View();
    }

    public ActionResult Me()
    {
      ViewBag.MySelf = GetMySelf();
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