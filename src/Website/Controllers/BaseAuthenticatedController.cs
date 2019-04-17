using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace Kcesar.MissionLine.Website.Controllers
{
  public class BaseAuthenticatedController : Controller
  {
    protected readonly IMemberSource Members;
    protected readonly IConfigSource Config;
    protected readonly ILog Log;

    public BaseAuthenticatedController(IMemberSource members, IConfigSource config, ILog log)
    {
      this.Members = members;
      this.Config = config;
      this.Log = log;
    }

    protected override ViewResult View(string viewName, string masterName, object model)
    {
      ViewBag.MySelf = GetMySelf();

      return base.View(viewName, masterName, model);
    }

    protected MemberLookupResult GetMySelf()
    {
      var identity = (ClaimsIdentity)User.Identity;
      var memberIdClaim = identity.FindFirst("memberId");
      return new MemberLookupResult { Id = memberIdClaim.Value, Name = identity.FindFirst("name").Value };
    }
  }
}