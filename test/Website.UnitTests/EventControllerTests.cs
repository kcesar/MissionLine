/*
 * Copyright 2015 Matthew Cosand
 */
namespace Website.UnitTests
{
  using System;
  using System.Linq;
  using Kcesar.MissionLine.Website.Api;
  using Kcesar.MissionLine.Website.Data;
  using NUnit.Framework;

  [TestFixture]
  public class EventsControllerTests
  {
    [Test]
    public void MergeNoSignins()
    {
      var context = TestContext.GetDefault<MergeTestContext>();
      var controller = new EventsController(() => context.DBMock.Object, context.ConfigMock.Object);
      var result = controller.Merge(context.From.Id, context.To.Id);
    }

    [TestCase("2015-01-01 04:00:00", null,                   // from        IN ------------
              "2015-01-01 03:00:00", "2015-01-01 04:00:05",  // into IN ---------- OUT
              "2015-01-01 03:00:00", null)]                  // rslt IN -------------------
    [TestCase("2015-01-01 03:00:00", "2015-01-01 04:00:05",  // from IN ---------- OUT
              "2015-01-01 04:00:00", null,                   // into        IN ------------
              "2015-01-01 03:00:00", null)]                  // rslt IN -------------------
    [TestCase("2015-01-01 03:00:00", "2015-01-01 04:00:05",  // from IN ---------------- OUT
              "2015-01-01 04:00:00", "2015-01-01 04:00:02",  // into        IN ------ OUT
              "2015-01-01 03:00:00", "2015-01-01 04:00:05")] // rslt IN ---------------- OUT
    [TestCase("2015-01-01 03:00:00", "2015-01-01 04:00:00",  // from IN ------------ OUT
              "2015-01-01 03:30:00", "2015-01-01 04:30:00",  // into        IN --------- OUT
              "2015-01-01 03:00:00", "2015-01-01 04:30:00")] // rslt IN ---------------- OUT
    public void MergeOverlappingSignins(params string[] inputs)
    {
      DateTime?[] dates = inputs.Select(f => string.IsNullOrWhiteSpace(f) ? (DateTime?)null : DateTime.Parse(f)).ToArray();
      var context = TestContext.GetDefault<MergeTestContext>();
      context.From.SignIns.Add(new MemberSignIn { Id = 123, MemberId = "MemberA", TimeIn = dates[0].Value, TimeOut = dates[1], Name = "Member A", EventId = context.From.Id });
      context.From.SignIns.Add(new MemberSignIn { Id = 124, MemberId = "MemberB", TimeIn = new DateTime(2015, 1, 1, 4, 0, 1), Name = "Member B", EventId = context.From.Id });
      context.To.SignIns.Add(new MemberSignIn { Id = 125, MemberId = "MemberA", TimeIn = dates[2].Value, TimeOut = dates[3], EventId = context.To.Id });

      var controller = new EventsController(() => context.DBMock.Object, context.ConfigMock.Object);
      var result = controller.Merge(context.From.Id, context.To.Id);

      Assert.AreEqual(2, context.To.SignIns.Count, "sign in count");
      var signin = context.To.SignIns.SingleOrDefault(f => f.MemberId == "MemberA");
      Assert.IsNotNull(signin, "1 sign in for memberId");
      Assert.AreEqual(dates[4].Value, signin.TimeIn, "time in");
      Assert.AreEqual(dates[5], signin.TimeOut, "time out");
    }



    class MergeTestContext : TestContext
    {
      public SarEvent From { get; private set; }
      public SarEvent To { get; private set; }
      protected override void DefaultSetup()
      {
        base.DefaultSetup();
        this.From = new SarEvent
        {
          Id = 5,
          Name = "Victim",
          Opened = new DateTime(2015, 1, 1),
        };
        this.To = new SarEvent
        {
          Id = 6,
          Name = "Winner",
          Opened = new DateTime(2015, 1, 2)
        };
        this.Events.Add(this.From);
        this.Events.Add(this.To);
      }
    }
  }
}
