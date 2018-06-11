namespace Kcesar.MissionLine.Website.Controllers
{
  using System.Security.Claims;
  using System.Web.Mvc;
  using log4net;

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
  }
}