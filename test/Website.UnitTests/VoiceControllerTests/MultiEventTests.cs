/*
 * Copyright 2015 Matthew Cosand
 */
namespace Website.UnitTests.VoiceControllerTests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Xml.Linq;
  using Kcesar.MissionLine.Website;
  using Kcesar.MissionLine.Website.Api;
  using Kcesar.MissionLine.Website.Data;
  using Kcesar.MissionLine.Website.Services;
  using Moq;
  using NUnit.Framework;

  [TestFixture]
  public class MultiEventTests
  {
    [Test]
    public void SigninPromptsForEvent()
    {
      var context = VoiceTestContext.GetDefault<VoiceTestContext>();
      var eventList = new List<SarEvent>();
      eventList.Add(new SarEvent { Id = 5, Name = "First Event" });
      eventList.Add(new SarEvent { Id = 6, Name = "Second Event" });
      context.EventsServiceMock.Setup(f => f.ListActive()).Returns(() => Task.Factory.StartNew(() => eventList));

      var result = Task.Run(() => context.DoApiCall(VoiceTestContext.AnswerUrl, (string)null)).Result.ToXDocument();

      Assert.AreEqual(1, context.Calls.Count(), "rows in call table");
      Assert.AreEqual(context.From, context.Calls.Single().Number, "stored phone number");
      Assert.AreEqual(0, context.SignIns.Count(), "rows in signin table");

      var menuAction = (from e in result.Descendants("Gather") select e.Attribute("action").Value).FirstOrDefault();
      Assert.IsTrue(menuAction.StartsWith("http://localhost/api/voice/DoMenu"), "action is DoMenu");

      result = Task.Run(() => context.DoApiCall(menuAction, "1")).Result.ToXDocument();

      var element = result.Root.FirstNode as XElement;
      Assert.AreEqual("Gather", element.Name.LocalName, "expected gather step 1");
      var url = element.Attribute("action").Value;

      // Should ask for more information
      Assert.AreEqual("SetEvent", context.GetActionMethod(url).Name, "next prompt is set time out");

      StringAssert.Contains(string.Format("Press {0} then pound for {1}.", 1, "First Event"), element.ToString(), "includes prompt for mission");
      Assert.AreEqual(1, context.SignIns.Count(), "sign in rows");
      Assert.AreEqual(null, context.SignIns.First().TimeOut, "time out is null");
      Assert.AreEqual(null, context.SignIns.First().EventId, "event id should be null when prompting");

      var uri = new Uri(url);
      result = Task.Run(() => context.DoApiCall(url, "1", true)).Result.ToXDocument();

      element = result.Root.FirstNode as XElement;
      Assert.AreEqual("Say", element.Name.LocalName, "expected say step 2");
      Assert.AreEqual(string.Format(Speeches.ChangeEventTemplate, "First Event"), element.Value, "change to event name");
      element = element.NextNode as XElement;
      Assert.AreEqual("Redirect", element.Name.LocalName, "action is a redirect");
      StringAssert.StartsWith("http://localhost/api/voice/menu?", element.Value.ToLower(), "redirects to menu");
      StringAssert.Contains("&e=5&", element.Value, "mission id is in url");
    }
  }
}
