/*
 * Copyright 2015 Matthew Cosand
 */
namespace Website.UnitTests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net.Http;
  using System.Threading.Tasks;
  using System.Web.Http;
  using System.Web.Http.Routing;
  using System.Xml.Linq;
  using Kcesar.MissionLine.Website;
  using Kcesar.MissionLine.Website.Api;
  using Kcesar.MissionLine.Website.Api.Model;
  using Kcesar.MissionLine.Website.Data;
  using Kcesar.MissionLine.Website.Services;
  using Moq;
  using Twilio.TwiML;

  public class VoiceTestContext : TestContext
  {
    protected override void DefaultSetup()
    {
      base.DefaultSetup();

      CallSid = Guid.NewGuid().ToString() + DateTime.Now.ToString();
      From = "+11234567890";
      Member = new MemberLookupResult { Id = Guid.NewGuid().ToString(), Name = "Mr. Sandman" };

      MembersMock = new Mock<IMemberSource>();
      MembersMock.Setup(f => f.LookupMemberPhone(this.From)).Returns(() => Task.Factory.StartNew<MemberLookupResult>(() => this.Member));

      EventsServiceMock = new Mock<IEventsService>(MockBehavior.Strict);
      EventsServiceMock.Setup(f => f.ListActive()).Returns(() => Task.Factory.StartNew<List<SarEvent>>(() => new List<SarEvent>()));
    }

    public TwilioRequest CreateRequest(string digits)
    {
      return new TwilioRequest { CallSid = this.CallSid, From = this.From, Digits = digits ?? string.Empty };
    }

    public Mock<IMemberSource> MembersMock { get; private set; }

    public Mock<IEventsService> EventsServiceMock { get; private set; }

    public MemberLookupResult Member { get; set; }

    public string From { get; private set; }

    public string CallSid { get; private set; }

    public async Task<TwilioResponse> DoApiCall(string action, string url = null, string digits = null, bool redirects = true)
    {
      var args = this.CreateRequest(digits);
      url = url ?? "http://localhost/api/voice/" + action;

      var controller = new VoiceController(() => this.DBMock.Object, this.EventsServiceMock.Object, this.ConfigMock.Object, this.MembersMock.Object, new ConsoleLogger());
      controller.Request = new HttpRequestMessage(HttpMethod.Post, url);
      controller.Configuration = new HttpConfiguration();

      WebApiConfig.Register(controller.Configuration);
      controller.RequestContext.RouteData = new HttpRouteData(
        route: new HttpRoute(),
        values: new HttpRouteValueDictionary { { "controller", "voice" }, { "action", action } });

      var collection = new System.Uri(url).ParseQueryString();
      var queries = collection.OfType<string>().ToDictionary(k => k, k => collection[k]);
      controller.InitBody(queries);

      Console.WriteLine("Posting to " + url);

      var method = typeof(VoiceController).GetMethod(action, new[] { typeof(TwilioRequest) });
      var result = await (Task<TwilioResponse>)(method.Invoke(controller, new object[] { args }));

      var first = result.ToXDocument().Root.FirstNode as XElement;
      if (redirects && first != null && first.Name == "Redirect")
      {
        result = await DoApiCall(GetActionFromUrl(first.Value), first.Value, null, true);
      }
      return result;
    }

    public string GetActionFromUrl(string url)
    {
      return url.Split('/', '?')[5];
    }
  }
}
