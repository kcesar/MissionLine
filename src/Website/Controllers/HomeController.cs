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
      ViewBag.LinkTemplate = ((ClaimsIdentity)User.Identity).FindFirst("profile")?.Value;
      ViewBag.Myself = GetMySelf();
      ViewBag.Signout = config.GetConfig("auth:endsession");
      return View();
    }

    public ActionResult Me()
    {
      ViewBag.MySelf = GetMySelf();
      ViewBag.Signout = config.GetConfig("auth:endsession");
      return View();
    }
  }
}