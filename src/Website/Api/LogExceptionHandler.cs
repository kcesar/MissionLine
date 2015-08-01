/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Api
{
  using System.Net.Http;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Web.Http;
  using log4net;

  public class LogExceptionHandler : DelegatingHandler
  {
    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      var response = await base.SendAsync(request, cancellationToken);

      var content = response.Content as ObjectContent<HttpError>;
      if (content != null)
      {
        var values = (HttpError)content.Value;
        var log = LogManager.GetLogger(string.Format("api.{0}controller", request.GetRouteData().Values["controller"]));
        log.ErrorFormat("{0}\r\n{1}\r\n{2}", values.ExceptionMessage, values.ExceptionType, values.StackTrace);
      }
      
      return response;
    }
  }
}