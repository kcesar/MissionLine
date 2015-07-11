using System;
using System.Linq;
using System.Threading.Tasks;
using Kcesar.MissionLine.Website;
using Kcesar.MissionLine.Website.Api.Model;
using Kcesar.MissionLine.Website.Data;
using Moq;

namespace Website.UnitTests
{
  public class TestContext
  {
    public static T GetDefault<T>() where T : TestContext, new()
    {
      var obj = new T();
      obj.DefaultSetup();
      return obj;
    }

    protected virtual void DefaultSetup()
    {
      Calls = new InMemoryDbSet<VoiceCall>();
      SignIns = new InMemoryDbSet<MemberSignIn>();
      Events = new InMemoryDbSet<SarEvent>();

      DBMock = new Mock<IMissionLineDbContext>(MockBehavior.Strict);

      DBMock.Setup(f => f.Dispose());
      DBMock.Setup(f => f.SaveChanges()).Returns(1);
      DBMock.Setup(f => f.SaveChangesAsync()).Returns(Task.Factory.StartNew<int>(() => 1));
      DBMock.SetupGet(f => f.Calls).Returns(Calls);
      DBMock.SetupGet(f => f.SignIns).Returns(SignIns);
      DBMock.SetupGet(f => f.Events).Returns(Events);

      ConfigMock = new Mock<IConfigSource>();
      ConfigMock.Setup(f => f.GetPushHub<CallsHub>()).Returns(new CallsHubImpl());
    }

    public Mock<IMissionLineDbContext> DBMock { get; private set; }
    public Mock<IConfigSource> ConfigMock { get; private set; }

    public InMemoryDbSet<MemberSignIn> SignIns { get; private set; }
    public InMemoryDbSet<VoiceCall> Calls { get; private set; }
    public InMemoryDbSet<SarEvent> Events { get; private set; }

    public class CallsHubImpl
    {
      public Action<CallEntry> updatedCall = (e => { });
      public Action<RosterEntry> updatedRoster = (e => { });
      public Action<EventEntry> updatedEvent = (e => { });
      public Action<int> removedEvent = (e => { });
    }

  }
}
