namespace Kcesar.MissionLine.Website.Controllers
{
  using System.Security.Claims;
  using System.Web.Mvc;
  using log4net;

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

    public ActionResult Index()
    {
      ViewBag.MySelf = GetMySelf();
      ViewBag.Signout = config.GetConfig("auth:endsession");
      return View();
    }

    private MemberLookupResult GetMySelf()
    {
      var identity = (ClaimsIdentity)User.Identity;
      var memberIdClaim = identity.FindFirst("memberId");
      return new MemberLookupResult { Id = memberIdClaim.Value, Name = identity.FindFirst("name").Value };
    }

    [Route("dashboard")]
    public ActionResult Dashboard()
    {
      ViewBag.LinkTemplate = config.GetConfig("memberLinkTemplate");
      ViewBag.Myself = GetMySelf();
      ViewBag.Signout = config.GetConfig("auth:endsession");
      return View();
    }
  }
}