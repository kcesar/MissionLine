using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using Kcesar.MissionLine.Website;
using Kcesar.MissionLine.Website.Api.Controllers;
using Kcesar.MissionLine.Website.Api.Model;
using Kcesar.MissionLine.Website.Controllers;
using Kcesar.MissionLine.Website.Data;
using Moq;
using NUnit.Framework;
using Twilio.TwiML;

namespace Website.UnitTests
{
  [TestFixture]
  public class VoiceControllerTests
  {
    [Test]
    public void QuickSignIn()
    {
      var callSid = Guid.NewGuid().ToString() + DateTime.Now.ToString();
      var fromPhone = "+11234567890";
      var member = new MemberLookupResult { Id = Guid.NewGuid().ToString(), Name = "Mr. Sandman" };

      var membersMock = new Mock<IMemberSource>();
      membersMock.Setup(f => f.LookupMemberPhone(fromPhone)).Returns(member);
      var calls = new InMemoryDbSet<VoiceCall>();
      var signins = new InMemoryDbSet<MemberSignIn>();
      
      var mockDb = new Mock<IMissionLineDbContext>(MockBehavior.Strict);
      mockDb.Setup(f => f.Dispose());
      mockDb.Setup(f => f.SaveChanges()).Returns(1);
      mockDb.SetupGet(f => f.Calls).Returns(calls);
      mockDb.SetupGet(f => f.SignIns).Returns(signins);


      var result = DoApiCall(
        "Answer",
        new TwilioRequest { From = fromPhone, CallSid = callSid },
        mockDb,
        membersMock);

      Assert.AreEqual(1, calls.Count(), "rows in call table");
      Assert.AreEqual(fromPhone, calls.Single().Number, "stored phone number");
      Assert.AreEqual(0, signins.Count(), "rows in signin table");

      var menuAction = (from e in result.ToXDocument().Descendants("Gather") select e.Attribute("action").Value).FirstOrDefault();
      Assert.IsTrue(menuAction.StartsWith("http://localhost/api/voice/DoMenu"), "action is DoMenu");

      result = DoApiCall(
        "DoMenu",
        menuAction,
        new TwilioRequest { From = fromPhone, CallSid = callSid, Digits = "1" },
        mockDb,
        membersMock);

      StringAssert.StartsWith("Signed in as Mr. Sandman", (from e in result.ToXDocument().Descendants("Say") select e.Value).FirstOrDefault(), "report signed in");
      Assert.AreEqual(1, signins.Count(), "sign in rows");
      Assert.AreEqual(null, signins.First().TimeOut, "time out is null");
      Assert.AreEqual(member.Name, signins.First().Name, "name");
      Assert.AreEqual(member.Id, signins.First().MemberId, "member id");
      Console.WriteLine(result);
    }

    private static TwilioResponse DoApiCall(string action, TwilioRequest args, Mock<IMissionLineDbContext> mockDb, Mock<IMemberSource> membersMock)
    {
      return DoApiCall(action, "http://localhost/api/voice/" + action.ToLowerInvariant(), args, mockDb, membersMock);
    }

    private static TwilioResponse DoApiCall(string action, string url, TwilioRequest args, Mock<IMissionLineDbContext> mockDb, Mock<IMemberSource> membersMock)
    {

      var configMock = new Mock<IConfigSource>();
      var controller = new VoiceController(() => mockDb.Object, configMock.Object, membersMock.Object);
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
