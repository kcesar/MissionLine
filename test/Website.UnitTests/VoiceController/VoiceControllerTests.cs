/*
 * Copyright 2015 Matthew Cosand
 */
namespace Website.UnitTests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Xml.Linq;
  using Kcesar.MissionLine.Website;
  using Kcesar.MissionLine.Website.Api;
  using Kcesar.MissionLine.Website.Data;
  using Moq;
  using NUnit.Framework;
  using Twilio.TwiML;

  [TestFixture]
  public class VoiceControllerTests
  {
    [Test]
    public void AnswerUnknownCallerNoMissions()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();
      context.Member = null;

      var expectedText = Speeches.WelcomeUnknownCaller
        + string.Format(Speeches.PressOneTemplate, string.Format(Speeches.PromptSignInTemplate, string.Empty))
        // no prompt to change missions because there are none
        + string.Format(Speeches.PromptRecordMessageTemplate, 3)
        + string.Format(Speeches.PromptChangeResponder, 8)
        + string.Format(Speeches.PromptAdminMenu, 9);

      AnswerCallCheckResult(context, expectedText);

    }

    [Test]
    public void AnswerKnownCallerNoMissions()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();

      string expectedText = GetKnownMemberSigninPrompt(context)
        + string.Format(Speeches.PromptRecordMessageTemplate, 3)
        + string.Format(Speeches.PromptChangeResponder, 8)
        + string.Format(Speeches.PromptAdminMenu, 9);
      AnswerCallCheckResult(context, expectedText);
    }

    private static string GetKnownMemberSigninPrompt(VoiceTestContext context)
    {
      var expectedText = string.Format(Speeches.PressOneTemplate,
            string.Format(Speeches.PromptSignInTemplate,
              string.Format(Speeches.AsMemberTemplate, context.Member.Name)));
        // no prompt to change missions because there are none
      return expectedText;
    }

    [Test]
    public void AnswerKnownCallerOneMission()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();
      var theEvent = new SarEvent { Id = 3, Name = "Test Event", Opened = DateTime.Now.AddHours(-4), OutgoingUrl = "https://example.com/sample.mp3" };
      context.EventsServiceMock
          .Setup(f => f.ListActive()).Returns(() => Task.FromResult(new List<SarEvent> { theEvent }));

      var expectedText = string.Format(Speeches.CurrentEventTemplate, theEvent.Name)
        + theEvent.OutgoingUrl
        + GetKnownMemberSigninPrompt(context)
        + string.Format(Speeches.PromptRecordMessageTemplate, 3)
        + string.Format(Speeches.PromptChangeResponder, 8)
        + string.Format(Speeches.PromptAdminMenu, 9);

      var response = AnswerCallCheckResult(context, expectedText);
      Assert.IsTrue(response.Descendants("Play").First().Value == theEvent.OutgoingUrl, "says outgoing message");
    }

    private static XDocument AnswerCallCheckResult(VoiceTestContext context, string expectedText)
    {
      var result = Task.Run(() => context.DoApiCall("Answer", (string)null)).Result.ToXDocument();
      var gather = result.Root.FirstNode as XElement;

      var menuAction = gather.Attribute("action").Value;
      Assert.IsTrue(menuAction.StartsWith("http://localhost/api/voice/DoMenu"), "action is DoMenu");

      if (expectedText != null)
      {
        Assert.AreEqual(expectedText, gather.Value, "menu text");
      }
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    [Test]
    public void QuickSignIn()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();

      var result = Task.Run(() => context.DoApiCall("Answer", (string)null)).Result;

      Assert.AreEqual(1, context.Calls.Count(), "rows in call table");
      Assert.AreEqual(context.From, context.Calls.Single().Number, "stored phone number");
      Assert.AreEqual(0, context.SignIns.Count(), "rows in signin table");

      var menuAction = (from e in result.ToXDocument().Descendants("Gather") select e.Attribute("action").Value).FirstOrDefault();
      Assert.IsTrue(menuAction.StartsWith("http://localhost/api/voice/DoMenu"), "action is DoMenu");

      result = Task.Run(() => context.DoApiCall("DoMenu", menuAction, "1")).Result;

      StringAssert.StartsWith("Signed in as Mr. Sandman", (from e in result.ToXDocument().Descendants("Say") select e.Value).FirstOrDefault(), "report signed in");
      Assert.AreEqual(1, context.SignIns.Count(), "sign in rows");
      Assert.AreEqual(null, context.SignIns.First().TimeOut, "time out is null");
      Assert.AreEqual(context.Member.Name, context.SignIns.First().Name, "name");
      Assert.AreEqual(context.Member.Id, context.SignIns.First().MemberId, "member id");
    }


    [Test]
    public void LoginForSignin()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();
      var memberNumber = "28594";
      context.MembersMock.Setup(f => f.LookupMemberDEM(memberNumber)).Returns(() => Task.FromResult(context.Member));
      context.MembersMock.Setup(f => f.LookupMemberPhone(It.IsAny<string>())).Returns(() => Task.FromResult<MemberLookupResult>(null));

      Assert.AreEqual(0, context.SignIns.Count(), "initial signins");

      // Setup: Unknown caller has dialed and initial message/menu has been presented.
      Task.Run(() => context.DoApiCall("Answer"));
      // Member now hits "1" to sign in:
      var result = Task.Run(() => context.DoApiCall("DoMenu", digits: "1")).Result.ToXDocument();

      // System says "Enter your DEM followed by the pound sign"
      var menuItem = result.Descendants("Say").Where(f => f.Value == Speeches.DEMPrompt).First();
      var url = menuItem.Parent.Attribute("action").Value;
      var action = context.GetActionFromUrl(url);

      Assert.AreEqual("DoLogin", action, "login action");

      // User enters their number:
      result = Task.Run(() => context.DoApiCall(action, url, memberNumber)).Result.ToXDocument();

      var signinConfirm = result.Descendants("Say").First().Value;
      Assert.IsTrue(signinConfirm.StartsWith(string.Format(Speeches.SignedInUnassignedTemplate, context.Member.Name, string.Empty)), "sign in confirmation");

      Assert.IsNotNull(context.SignIns.SingleOrDefault(f => f.EventId == null
                                                            && f.MemberId == context.Member.Id
                                                            && f.Name == context.Member.Name
                                                            && f.TimeOut == null
                                                            ));
    }

    /// <summary>
    /// 
    /// </summary>
    [Test]
    public void UpdateSigninStatusOnAnswer()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();

      context.SignIns.Add(new MemberSignIn { MemberId = context.Member.Id, TimeIn = DateTime.Now.AddMinutes(-5), Id = 5 });

      var result = Task.Run(() => context.DoApiCall("Answer", (string)null)).Result;

      Assert.IsNotNull((from e in result.ToXDocument().Descendants("Gather")
                        from a in e.Attributes()
                        where a.Name == "action" && a.Value.Contains(BaseVoiceController.QueryFields.SignedInKey + "=1")
                        select a).SingleOrDefault(),
                        "action has 'signed in' query value");

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

      var result = Task.Run(() => context.DoApiCall("DoLogin", digits: dem)).Result.ToXDocument();

      Console.WriteLine(result);

      var element = result.Root.FirstNode as XElement;
      Assert.AreEqual("Gather", element.Name.LocalName, "expected gather step");

      var url = element.Attribute("action").Value;
      Assert.True(url.Contains(VoiceController.QueryFields.SignedInKey + "=1"), "action has isSignedIn query value: " + url);

      string firstOption = element.Descendants("Say").First().Value;
      Assert.True(firstOption.Contains("sign out"), firstOption + " contains text 'sign out'");
      Assert.True(firstOption.Contains(otherMember.Name), firstOption + " contains new member name");
    }

    private const string RecordMessageKey = "3";

    [Test]
    public void RecordInMenu()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();

      var result = Task.Run(() => context.DoApiCall("Answer")).Result;

      Assert.IsTrue(result.ToString().Contains(string.Format(Speeches.PromptRecordMessageTemplate, RecordMessageKey)), "answer menu has record prompt");
    }

    [Test]
    public void RecordMessagePrompt()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();

      var result = Task.Run(() => context.DoApiCall("Answer")).Result;

      var nodes = Task.Run(() => context.DoApiCall("DoMenu", digits: RecordMessageKey)).Result.ToXDocument()
                      .Root.Descendants().Cast<XElement>().ToArray();

      Assert.AreEqual(Speeches.StartRecording, nodes[0].Value, "record prompt");

      var recordNode = nodes[1];
      Assert.AreEqual("Record", recordNode.Name.LocalName, "tag name");
      Assert.AreEqual("http://localhost/api/voice/StopRecording", recordNode.Attribute("action").Value, "action value");
    }

    [Test]
    public void FinishRecordMessage()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();

      string recordingUrl = "http://somelocation.tld/" + Guid.NewGuid().ToString();
      int recordingDuration = 43;

      var request = context.CreateRequest(null);
      request.RecordingUrl = recordingUrl;
      request.RecordingDuration = recordingDuration;

      var result = Task.Run(() => context.DoApiCall("Answer")).Result;
      
      result = Task.Run(() => context.DoApiCall("StopRecording", request)).Result;

      Assert.AreEqual(recordingDuration, context.Calls.Single().RecordingDuration, "duration");
      Assert.AreEqual(recordingUrl, context.Calls.Single().RecordingUrl, "url");
    }

    [Test]
    public void Signout()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();
      var signin = new MemberSignIn {
        Id = 24,
        isMember = true,
        MemberId = context.Member.Id,
        Name = context.Member.Name,
        TimeIn = DateTime.Now.AddHours(-7.5)
      };
      context.SignIns.Add(signin);

      string expectedAnswer = string.Format(Speeches.PressOneTemplate,
        string.Format(Speeches.PromptSignOutTemplate, string.Format(Speeches.AsMemberTemplate, context.Member.Name)))
        + string.Format(Speeches.PromptRecordMessageTemplate, 3)
        + string.Format(Speeches.PromptChangeResponder, 8)
        + string.Format(Speeches.PromptAdminMenu, 9);


      var result = AnswerCallCheckResult(context, expectedAnswer);
      var element = result.Root.FirstNode as XElement;
      Assert.AreEqual("Gather", element.Name.LocalName, "expected answer gather step");
      var url = element.Attribute("action").Value;


      DateTime markerA = TimeUtils.GetLocalDateTime(context.ConfigMock.Object);
      result = Task.Run(() => context.DoApiCall("DoMenu", url, digits: "1")).Result.ToXDocument();

      // Should mark the member signed out in the database
      Assert.IsTrue(signin.TimeOut.HasValue
           && signin.TimeOut.Value <= TimeUtils.GetLocalDateTime(context.ConfigMock.Object)
           && signin.TimeOut.Value >= markerA, "default time out in range");
      var defaultTimeout = signin.TimeOut.Value;

      element = result.Root.FirstNode as XElement;
      Assert.AreEqual("Gather", element.Name.LocalName, "expected gather step 1");
      url = element.Attribute("action").Value;

      // Should ask for more information
      Assert.AreEqual("SetTimeOut", context.GetActionFromUrl(url), "next prompt is set time out");

      // The call should be tracking the caller as signed out
      Assert.False(url.Contains(VoiceController.QueryFields.SignedInKey + "=1"), "action does not have signed in query value");

      // Update the time out for an hour from now.
      result = Task.Run(() => context.DoApiCall("SetTimeOut", url, "60")).Result.ToXDocument();
      Assert.AreEqual(
        string.Format(Speeches.SignedOutTemplate, context.Member.Name, TimeUtils.GetMiltaryTimeVoiceText(signin.TimeOut.Value)),
        result.Descendants("Say").First().Value,
        "report updated time out");
      Assert.AreEqual(defaultTimeout.AddMinutes(60), signin.TimeOut.Value, "updated time out");

      element = result.Root.FirstNode as XElement;
      Assert.AreEqual("Gather", element.Name.LocalName, "expected gather step 2");
      url = element.Attribute("action").Value;

      Assert.AreEqual("SetMiles", context.GetActionFromUrl(url), "next prompt is set miles");

      // The caller updates their miles
      result = Task.Run(() => context.DoApiCall("SetMiles", url, "120")).Result.ToXDocument();
      Assert.AreEqual(Speeches.MilesUpdated, result.Descendants("Say").First().Value, "report updated miles");
      Assert.AreEqual(120, signin.Miles.Value, "updated miles");
    }
  }
}
