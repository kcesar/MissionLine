namespace Kcesar.MissionLine.Website.Controllers
{
  using System.Security.Claims;
  using System.Web.Mvc;
  using log4net;
  using System.Web;

  public class HomeController : BaseAuthenticatedController
  {

    public HomeController(IMemberSource members, IConfigSource config, ILog log) : base(members, config, log)
    {
    }

    public ActionResult Index()
    {
      return View();
    }

    [Route("dashboard")]
    public ActionResult Dashboard()
    {
      ViewBag.LinkTemplate = Config.GetConfig("memberLinkTemplate");

      return View();
    }

    [Route("logout")]
    public ActionResult Logout()
    {
      string endSessionUrl = Config.GetConfig("auth:authority") + "/connect/endsession?id_token=" + ((ClaimsPrincipal)User).FindFirst("id_token")?.Value;
      HttpContext.GetOwinContext().Authentication.SignOut("Cookies");
      return Redirect(endSessionUrl);
    }
  }
}