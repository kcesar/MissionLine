/*
 * Copyright 2015 Matthew Cosand
 */
namespace Website.UnitTests
{
  using System;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Xml.Linq;
  using Kcesar.MissionLine.Website;
  using Kcesar.MissionLine.Website.Api.Controllers;
  using Kcesar.MissionLine.Website.Data;
  using NUnit.Framework;
  using Twilio.TwiML;

  [TestFixture]
  public class VoiceControllerTests
  {
    /// <summary>
    /// 
    /// </summary>
    [Test]
    public void QuickSignIn()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();

      var result = Task.Run(() => context.DoApiCall("Answer", null)).Result;

      Assert.AreEqual(1, context.Calls.Count(), "rows in call table");
      Assert.AreEqual(context.From, context.Calls.Single().Number, "stored phone number");
      Assert.AreEqual(0, context.SignIns.Count(), "rows in signin table");

      var menuAction = (from e in result.ToXDocument().Descendants("Gather") select e.Attribute("action").Value).FirstOrDefault();
      Assert.IsTrue(menuAction.StartsWith("http://localhost/api/voice/DoMenu"), "action is DoMenu");

      result = Task.Run(() => context.DoApiCall("DoMenu", menuAction, context.CreateRequest("1"))).Result;

      StringAssert.StartsWith("Signed in as Mr. Sandman", (from e in result.ToXDocument().Descendants("Say") select e.Value).FirstOrDefault(), "report signed in");
      Assert.AreEqual(1, context.SignIns.Count(), "sign in rows");
      Assert.AreEqual(null, context.SignIns.First().TimeOut, "time out is null");
      Assert.AreEqual(context.Member.Name, context.SignIns.First().Name, "name");
      Assert.AreEqual(context.Member.Id, context.SignIns.First().MemberId, "member id");
    }

    /// <summary>
    /// 
    /// </summary>
    [Test]
    public void UpdateSigninStatusOnAnswer()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();

      context.SignIns.Add(new MemberSignIn { MemberId = context.Member.Id, TimeIn = DateTime.Now.AddMinutes(-5), Id = 5 });

      var result = Task.Run(() => context.DoApiCall("Answer", null)).Result;

      Assert.IsNotNull((from e in result.ToXDocument().Descendants("Gather")
                        from a in e.Attributes()
                        where a.Name == "action" && a.Value.Contains("isS=1")
                        select a).SingleOrDefault(),
                        "action has isS query value");
      
      Assert.IsNotNull(result.ToXDocument().Descendants("Say").First().Value.Contains("sign out"), "contains text 'sign out'");
    }

    /// <summary>
    /// 
    /// </summary>
    [Test]
    public void UpdateSignStatusOnLogin()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();
      string dem = "1234";

      var otherMember = new MemberLookupResult { Id = "alternate", Name = "Fuzzy Bunny" };

      context.SignIns.Add(new MemberSignIn { MemberId = otherMember.Id, TimeIn = DateTime.Now.AddMinutes(-5), Id = 5 });
      context.MembersMock.Setup(f => f.LookupMemberDEM(dem)).Returns(Task.Factory.StartNew<MemberLookupResult>(() => otherMember));

      var result = Task.Run(() => context.DoApiCall("DoLogin", dem)).Result;

      Console.WriteLine(result);

      var action = (from e in result.ToXDocument().Descendants("Gather")
                        from a in e.Attributes()
                        where a.Name == "action" select a.Value).Single();

      Assert.IsNotNull(action.Contains("isS=1"), "action has isS query value: " + action);

      string firstOption = result.ToXDocument().Descendants("Gather").First().Descendants("Say").First().Value;
      Assert.True(firstOption.Contains("sign out"), firstOption + " contains text 'sign out'");
      Assert.True(firstOption.Contains(otherMember.Name), firstOption + " contains new member name");

    }
  }
}
