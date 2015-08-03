/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Controllers
{
  using System.Web.Mvc;

  public class HomeController : Controller
  {
    private readonly IConfigSource config;

    public HomeController(IConfigSource config)
    {
      this.config = config;
    }

    public ActionResult Index()
    {
      ViewBag.LinkTemplate = this.config.GetConfig("memberLinkTemplate");
      return View();
    }
  }
}