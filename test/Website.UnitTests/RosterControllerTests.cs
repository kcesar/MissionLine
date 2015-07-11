using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kcesar.MissionLine.Website;
using Kcesar.MissionLine.Website.Api;
using Kcesar.MissionLine.Website.Data;
using Moq;
using NUnit.Framework;

namespace Website.UnitTests
{
  [TestFixture]
  public class RosterControllerTests
  {
    [TestCase("04:00", "05:00",
      new[] { "03:00", "04:15", "04:45", "05:30" },
      new[] { "03:00", "04:00", "*04:00", "04:45", "04:45", "05:30" },
      TestName = "Two overlaps front and back")]
    [TestCase("04:00", null,
      new[] { "05:00", "06:00" },
      new[] { "04:00", "05:00", "05:00", "06:00", "*06:00", null },
      TestName = "Completely overlaps results in split")]
    public void Assign(string movingIn, string movingOut, string[] existingIns, string[] expectedOuts)
    {
      string memberId = "the member";

      var context = TestContext.GetDefault<TestContext>();
      var membersMock = new Mock<IMemberSource>(MockBehavior.Strict);
      var controller = new RosterController(() => context.DBMock.Object, context.ConfigMock.Object, membersMock.Object);

      int eventId = 52;
      context.Events.Add(new SarEvent { Id = eventId, Opened = "3:00".TimeToDate().Value, Name = "Test Event" });
      for (int i=0;i<existingIns.Length;i+=2)
      {
        context.SignIns.Add(new MemberSignIn { Id = i, MemberId = memberId, TimeIn = existingIns[i].TimeToDate().Value, TimeOut = existingIns[i + 1].TimeToDate(), EventId = eventId });
      }

      var target = new MemberSignIn { Id = 54, MemberId = memberId, EventId = null, TimeIn = movingIn.TimeToDate().Value, TimeOut = movingOut.TimeToDate() };
      context.SignIns.Add(target);

      var apiResult = Task.Run(() => controller.Assign(target.Id, eventId)).Result;
      Assert.AreEqual(0, apiResult.Errors.Count, "expect no errors: " + string.Join("\n", apiResult.Errors.Select(f => f.Text)));
      Assert.AreEqual(0, context.SignIns.Count(f => f.EventId == null), "no leftovers");
      var results = context.SignIns.OrderBy(f => f.TimeIn).ToList();
      Assert.AreEqual(expectedOuts.Length / 2, results.Count, "expected results");
      for (int i = 0; i < results.Count; i++)
      {
        Assert.AreEqual(expectedOuts[2 * i][0] == '*', results[i] == target, "target is in the right place");
        Assert.AreEqual(expectedOuts[2*i].Trim('*').TimeToDate(), results[i].TimeIn, "expected time in " + i.ToString());
        Assert.AreEqual(expectedOuts[2 * i + 1].TimeToDate(), results[i].TimeOut, "expected time out " + i.ToString());
        Assert.AreEqual(eventId, results[i].EventId, "is assigned to event " + i.ToString());
        Assert.AreEqual(memberId, results[i].MemberId, "correct memberid " + i.ToString());
      }
    }


    //[TestCase("2015-01-01 04:00:00", null,                   // from        IN ------------
    //          "2015-01-01 03:00:00", "2015-01-01 04:00:05",  // into IN ---------- OUT
    //          "2015-01-01 03:00:00", null)]                  // rslt IN -------------------
    //[TestCase("2015-01-01 03:00:00", "2015-01-01 04:00:05",  // from IN ---------- OUT
    //          "2015-01-01 04:00:00", null,                   // into        IN ------------
    //          "2015-01-01 03:00:00", null)]                  // rslt IN -------------------
    //[TestCase("2015-01-01 03:00:00", "2015-01-01 04:00:05",  // from IN ---------------- OUT
    //          "2015-01-01 04:00:00", "2015-01-01 04:00:02",  // into        IN ------ OUT
    //          "2015-01-01 03:00:00", "2015-01-01 04:00:05")] // rslt IN ---------------- OUT
    //[TestCase("2015-01-01 03:00:00", "2015-01-01 04:00:00",  // from IN ------------ OUT
    //          "2015-01-01 03:30:00", "2015-01-01 04:30:00",  // into        IN --------- OUT
    //          "2015-01-01 03:00:00", "2015-01-01 04:30:00")] // rslt IN ---------------- OUT
    //public void MergeOverlappingSignins(params string[] inputs)
    //{
    //  DateTime?[] dates = inputs.Select(f => string.IsNullOrWhiteSpace(f) ? (DateTime?)null : DateTime.Parse(f)).ToArray();
    //  var context = TestContext.GetDefault<MergeTestContext>();
    //  context.From.SignIns.Add(new MemberSignIn { Id = 123, MemberId = "MemberA", TimeIn = dates[0].Value, TimeOut = dates[1], Name = "Member A", EventId = context.From.Id });
    //  context.From.SignIns.Add(new MemberSignIn { Id = 124, MemberId = "MemberB", TimeIn = new DateTime(2015, 1, 1, 4, 0, 1), Name = "Member B", EventId = context.From.Id });
    //  context.To.SignIns.Add(new MemberSignIn { Id = 125, MemberId = "MemberA", TimeIn = dates[2].Value, TimeOut = dates[3], EventId = context.To.Id });

    //  var controller = new EventsController(() => context.DBMock.Object, context.ConfigMock.Object);
    //  var result = controller.Merge(context.From.Id, context.To.Id);

    //  Assert.AreEqual(2, context.To.SignIns.Count, "sign in count");
    //  var signin = context.To.SignIns.SingleOrDefault(f => f.MemberId == "MemberA");
    //  Assert.IsNotNull(signin, "1 sign in for memberId");
    //  Assert.AreEqual(dates[4].Value, signin.TimeIn, "time in");
    //  Assert.AreEqual(dates[5], signin.TimeOut, "time out");
    //}
  }
}
