/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Api
{
  using System.Collections.Generic;
  using System.Net.Http;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Web.Http;
  using System.Web.Http.ExceptionHandling;
  using log4net;
  using Newtonsoft.Json;

  public class Log4NetExceptionLogger : ExceptionLogger
  {
    public override void Log(ExceptionLoggerContext context)
    {
      var log = LogManager.GetLogger(string.Format("api.{0}controller", context.Request.GetRouteData().Values["controller"]));
      log.Error("Unhandled exception", context.Exception);
    }
  }
}