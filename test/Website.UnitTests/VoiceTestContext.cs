using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;
using Kcesar.MissionLine.Website;
using Kcesar.MissionLine.Website.Api.Controllers;
using Kcesar.MissionLine.Website.Api.Model;
using Kcesar.MissionLine.Website.Data;
using Moq;
using Twilio.TwiML;

namespace Website.UnitTests
{
  public class VoiceTestContext
  {
    public static VoiceTestContext GetDefault()
    {
      var context = new VoiceTestContext
      {
        CallSid = Guid.NewGuid().ToString() + DateTime.Now.ToString(),
        From = "+11234567890",
        Member = new MemberLookupResult { Id = Guid.NewGuid().ToString(), Name = "Mr. Sandman" },

        Calls = new InMemoryDbSet<VoiceCall>(),
        SignIns = new InMemoryDbSet<MemberSignIn>(),

        MembersMock = new Mock<IMemberSource>(),
        DBMock = new Mock<IMissionLineDbContext>(MockBehavior.Strict)
      };

      context.MembersMock.Setup(f => f.LookupMemberPhone(context.From)).Returns(context.Member);

      context.DBMock.Setup(f => f.Dispose());
      context.DBMock.Setup(f => f.SaveChanges()).Returns(1);
      context.DBMock.SetupGet(f => f.Calls).Returns(context.Calls);
      context.DBMock.SetupGet(f => f.SignIns).Returns(context.SignIns);

      return context;
    }

    public TwilioRequest CreateRequest(string digits)
    {
      return new TwilioRequest { CallSid = this.CallSid, From = this.From, Digits = digits ?? string.Empty };
    }

    public Mock<IMissionLineDbContext> DBMock { get; private set; }

    public Mock<IMemberSource> MembersMock { get; private set; }

    public InMemoryDbSet<MemberSignIn> SignIns { get; private set; }

    public InMemoryDbSet<VoiceCall> Calls { get; private set; }

    public MemberLookupResult Member { get; private set; }

    public string From { get; private set; }

    public string CallSid { get; private set; }

    public TwilioResponse DoApiCall(string action, string digits)
    {
      return DoApiCall(action, "http://localhost/api/voice/" + action.ToLowerInvariant(), this.CreateRequest(digits));
    }

    public TwilioResponse DoApiCall(string action, string url, TwilioRequest args)
    {

      var configMock = new Mock<IConfigSource>();
      var controller = new VoiceController(() => this.DBMock.Object, configMock.Object, this.MembersMock.Object);
      controller.Request = new HttpRequestMessage(HttpMethod.Post, url);
      controller.Configuration = new HttpConfiguration();
      WebApiConfig.Register(controller.Configuration);
      controller.RequestContext.RouteData = new HttpRouteData(
        route: new HttpRoute(),
        values: new HttpRouteValueDictionary { { "controller", "voice" }, { "action", action } });

      var collection = new System.Uri(url).ParseQueryString();
      var queries = collection.OfType<string>().ToDictionary(k => k, k => collection[k]);
      controller.ParseQuery(queries);

      Console.WriteLine("Posting to " + url);

      var method = typeof(VoiceController).GetMethod(action, new[] { typeof(TwilioRequest) });
      var result = (TwilioResponse)(method.Invoke(controller, new object[] { args }));
      return result;
    }
  }
}
