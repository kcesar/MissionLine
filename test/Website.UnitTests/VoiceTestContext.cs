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
  using Kcesar.MissionLine.Website.Api.Controllers;
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
      MembersMock.Setup(f => f.LookupMemberPhone(this.From)).Returns(Task.Factory.StartNew<MemberLookupResult>(() => this.Member));

    }

    public TwilioRequest CreateRequest(string digits)
    {
      return new TwilioRequest { CallSid = this.CallSid, From = this.From, Digits = digits ?? string.Empty };
    }

    public Mock<IMemberSource> MembersMock { get; private set; }

    public MemberLookupResult Member { get; private set; }

    public string From { get; private set; }

    public string CallSid { get; private set; }

    public Task<TwilioResponse> DoApiCall(string action, string digits)
    {
      return DoApiCall(action, "http://localhost/api/voice/" + action.ToLowerInvariant(), this.CreateRequest(digits));
    }

    public async Task<TwilioResponse> DoApiCall(string action, string url, TwilioRequest args)
    {
      var controller = new VoiceController(() => this.DBMock.Object, this.ConfigMock.Object, this.MembersMock.Object);
      controller.Request = new HttpRequestMessage(HttpMethod.Post, url);
      controller.Configuration = new HttpConfiguration();
      WebApiConfig.Register(controller.Configuration);
      controller.RequestContext.RouteData = new HttpRouteData(
        route: new HttpRoute(),
        values: new HttpRouteValueDictionary { { "controller", "voice" }, { "action", action } });

      var collection = new System.Uri(url).ParseQueryString();
      var queries = collection.OfType<string>().ToDictionary(k => k, k => collection[k]);
      controller.session.Load(queries);

      Console.WriteLine("Posting to " + url);

      var method = typeof(VoiceController).GetMethod(action, new[] { typeof(TwilioRequest) });
      var result = await (Task<TwilioResponse>)(method.Invoke(controller, new object[] { args }));
      return result;
    }
  }
}
