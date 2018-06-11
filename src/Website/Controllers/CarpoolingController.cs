using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using log4net;

namespace Kcesar.MissionLine.Website.Controllers
{
  public class CarpoolingController : BaseAuthenticatedController
  {
    public CarpoolingController(IMemberSource members, IConfigSource config, ILog log) : base(members, config, log)
    {
    }

    [Route("events/{eventId}/carpooling")]
    public ActionResult Index(int eventId)
    {
      ViewBag.EventId = eventId;
      ViewBag.FullHeightBody = true;

      return View();
    }
  }
}
