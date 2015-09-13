/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Controllers
{
  using System.Web.Mvc;
  using log4net;

  [RequireHttps]
  public class HomeController : Controller
  {
    private readonly IConfigSource config;
    private readonly ILog log;

    public HomeController(IConfigSource config, ILog log)
    {
      this.config = config;
      this.log = log;
    }

    public ActionResult Index()
    {
      ViewBag.LinkTemplate = this.config.GetConfig("memberLinkTemplate");
      return View();
    }

    public ActionResult Heartbeat()
    {
      log.Info("Heartbeat");
      return Content("OK");
    }
  }
}