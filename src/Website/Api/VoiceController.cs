/*
 * Copyright 2015 Matt Cosand
 */
namespace Kcesar.MissionLine.Website.Api.Controllers
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net.Http;
  using System.Web;
  using System.Web.Http;
  using Kcesar.MissionLine.Website.Api.Model;
  using Kcesar.MissionLine.Website.Data;
  using Microsoft.AspNet.SignalR;
  using Twilio.TwiML;

  /// <summary>
  /// 
  /// </summary>
  [UseTwilioFormatter]
  public class VoiceController : ApiController
  {
    private string memberId = null;
    private string memberName = null;
    private bool hasRecording = false;
    private bool isSignedIn = false;

    private readonly IConfigSource config;
    private readonly IMemberSource members;
    private readonly Func<IMissionLineDbContext> dbFactory;

    private dynamic CallHubClients { get { return GlobalHost.ConnectionManager.GetHubContext<CallsHub>().Clients.All;  } }

    /// <summary>
    /// 
    /// </summary>
    public VoiceController()
      : this(() => new MissionLineDbContext(), new ConfigSource(), new MemberSource(new ConfigSource()))
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbFactory"></param>
    /// <param name="config"></param>
    /// <param name="members"></param>
    public VoiceController(Func<IMissionLineDbContext> dbFactory, IConfigSource config, IMemberSource members)
    {
      this.dbFactory = dbFactory;
      this.config = config;
      this.members = members;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public TwilioResponse Test()
    {
      var r = new TwilioResponse();
      r.Say("This is a test");
      return r;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public TwilioResponse Answer(TwilioRequest request)
    {
      SetMemberInfoFromPhone(request.From);

      Db(db =>
      {
        var call = new VoiceCall
        {
          CallId = request.CallSid,
          Number = request.From,
          CallTime = GetLocalDateTime(),
          Name = memberName
        };

        db.Calls.Add(call);
        db.SaveChanges();
      });

      var response = BeginMenu();
      if (this.memberId == null)
      {
        response.SayVoice(Speeches.WelcomeUnknownCaller);
      }

      EndMenu(response);

      return response;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public TwilioResponse DoMenu(TwilioRequest request)
    {
      var response = new TwilioResponse();

      if (request.Digits == "1")
      {
        if (this.memberId == null)
        {
          AddLoginPrompt(response);
        }
        else
        {
          SignInOrOut(response, request.CallSid);
        }
      }
      else if (request.Digits == "2")
      {
        response.SayVoice(Speeches.StartRecording);
        response.Record(new { maxLength = 120, action = GetAction("StopRecording") });
        BeginMenu(response);
        EndMenu(response);
      }
      else if (request.Digits == "3")
      {
        AddLoginPrompt(response);
      }
      else
      {
        response.SayVoice("I didn't understand.");
        BeginMenu(response);
        EndMenu(response);
      }

      return response;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public TwilioResponse DoLogin(TwilioRequest request)
    {
      var response = new TwilioResponse();

      var lookup = members.LookupMemberDEM(request.Digits);
      if (lookup != null)
      {
        this.memberId = lookup.Id;
        this.memberName = lookup.Name;
        BeginMenu(response);
        EndMenu(response);
      }
      else
      {
        AddLoginPrompt(response);
      }

      return response;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public TwilioResponse SetMiles(TwilioRequest request)
    {
      int miles;
      if (int.TryParse(request.Digits, out miles))
      {
        Db(db =>
        {
          var signin = db.SignIns.OrderByDescending(f => f.TimeIn).FirstOrDefault(f => f.MemberId == memberId);
          signin.Miles = miles;
          db.SaveChanges();
          CallHubClients.updatedRoster(RosterController.GetRosterEntry(signin.Id, db));
        });
      }

      var response = BeginMenu();
      response.SayVoice("Your miles have been updated.");
      EndMenu(response, true);
      return response;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public TwilioResponse StopRecording(TwilioRequest request)
    {
      TwilioResponse response = null;
      Db(db =>
      {
        var call = db.Calls.Single(f => f.CallId == request.CallSid);
        //if (!string.IsNullOrWhiteSpace(call.RecordingUrl))
        //{
        //  // Delete previous recording
        //}
        call.RecordingDuration = request.RecordingDuration;
        call.RecordingUrl = request.RecordingUrl;
        db.SaveChanges();

        response = BeginMenu();
        response.SayVoice("Recording saved.");
        EndMenu(response, true);
      });
      return response;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public TwilioResponse Complete(TwilioRequest request)
    {
      Db(db =>
      {
        var call = db.Calls.Where(f => f.CallId == request.CallSid).SingleOrDefault();
        if (call != null)
        {
          call.Duration = request.CallDuration;
          db.SaveChanges();
        }
      });

      var response = new TwilioResponse();
      response.Hangup();
      return response;
    }

    // =========================================  END PUBLIC METHODS  =============================================


    private void SignInOrOut(TwilioResponse response, string callId)
    {
      Db(db =>
      {
        DateTime time = GetLocalDateTime();
        var sayDate = GetMiltaryTimeText(time);


        // Get the last time the responder signed in or out.
        var signin = db.SignIns.Where(f => f.MemberId == this.memberId).OrderByDescending(f => f.TimeIn).FirstOrDefault();
        var call = db.Calls.Single(f => f.CallId == callId);

        // If they've never signed in or have already signed out:
        if (signin == null || signin.TimeOut.HasValue)
        {
          if (this.isSignedIn)
          {
            throw new InvalidOperationException("Tried to sign out when not signed in.");
          }

          // Sign them in.
          signin = new MemberSignIn
          {
            MemberId = this.memberId,
            Name = this.memberName,
            TimeIn = time
          };
          db.SignIns.Add(signin);
          call.Actions.Add(new CallAction { Call = call, CallId = call.Id, Time = time, Action = "Signed in " + this.memberName });
          this.isSignedIn = true;
          BeginMenu(response);
          response.SayVoice(string.Format("Signed in as {0} at {1}", this.memberName, sayDate));
          EndMenu(response);
        }
        else
        {
          signin.TimeOut = time;
          call.Actions.Add(new CallAction { Call = call, CallId = call.Id, Time = time, Action = "Signed out " + this.memberName });
          this.isSignedIn = false;
          // add prompt for miles
          response.BeginGather(new { timeout = 10, action = GetAction("SetMiles") });
          response.SayVoice("{0} signed out at {1}. Enter your miles followed by the pound key. Press pound if you did not drive.", this.memberName, sayDate);
          response.EndGather();
        }
        db.SaveChanges();
        CallHubClients.updatedRoster(RosterController.GetRosterEntry(signin.Id, db));
      });
    }

    protected override void Initialize(System.Web.Http.Controllers.HttpControllerContext controllerContext)
    {
      base.Initialize(controllerContext);

      ParseQuery(controllerContext.Request.GetQueryNameValuePairs());
    }

    public void ParseQuery(IEnumerable<KeyValuePair<string, string>> queries)
    {
      this.memberId = queries.Where(f => f.Key == "memberId").Select(f => f.Value).FirstOrDefault();
      this.memberName = queries.Where(f => f.Key == "memberName").Select(f => f.Value).FirstOrDefault();
      this.hasRecording = queries.Where(f => f.Key == "hasR").Select(f => f.Value).FirstOrDefault() == "1";
      this.isSignedIn = queries.Where(f => f.Key == "isS").Select(f => f.Value).FirstOrDefault() == "1";
    }

    private TwilioResponse BeginMenu()
    {
      var response = new TwilioResponse();
      BeginMenu(response);
      return response;
    }

    private void BeginMenu(TwilioResponse response)
    {
      response.BeginGather(new { numDigits = 1, action = GetAction("DoMenu"), timeout = 10 });
    }

    private void EndMenu(TwilioResponse response, bool isContinuation = false)
    {
      if (isContinuation)
      {
        response.SayVoice("You may hang up or ");
      }
      string sayAsMember = this.memberName != null ? (" as " + this.memberName) : string.Empty;
      response.SayVoice("Press 1 to sign {0}{1}", this.isSignedIn ? "out" : "in", sayAsMember);
      response.SayVoice(this.hasRecording ? "Press 2 to record a new message." : "Press 2 to record a message.");
      response.SayVoice("Press 3 to change current responder");
      response.EndGather();
    }

    private void Db(Action<IMissionLineDbContext> action)
    {
      using (var db = dbFactory())
      {
        action(db);
      }
    }


    private DateTime GetLocalDateTime()
    {
      return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, this.config.GetConfig("timezone") ?? "Pacific Standard Time");
    }


    private static string GetMiltaryTimeText(DateTime time)
    {
      // "m" is giving the same result as "M" (month + day)
      string minuteText = time.ToString("mm").TrimStart('0');
      if (time.Minute == 0)
      {
        minuteText = "hundred ";
      }
      else if (time.Minute < 10)
      {
        minuteText = "oh " + minuteText;
      }
      return time.ToString("H ") + minuteText;
    }

    private void SetMemberInfoFromPhone(string From)
    {
      var lookup = members.LookupMemberPhone(From);
      this.memberId = lookup.Id;
      this.memberName = lookup.Name;
    }

    private void AddLoginPrompt(TwilioResponse response)
    {
      response.BeginGather(new { timeout = 10, action = GetAction("DoLogin") });
      response.SayVoice("Enter your D E M number followed by the pound key.");
      response.SayVoice("To go back, press pound");
      response.EndGather();
      BeginMenu(response);
      EndMenu(response);
    }

    private string GetAction(string name)
    {
      Dictionary<string, string> args = new Dictionary<string, string>();
      if (memberId != null)
      {
        args.Add("memberId", memberId);
        args.Add("memberName", memberName);
      }
      if (this.hasRecording)
      {
        args.Add("hasR", "1");
      }
      if (this.isSignedIn)
      {
        args.Add("isS", "1");
      }

      string result = this.Url.Content("~/api/voice/" + name);
      if (args.Count > 0)
      {
        result += "?" + string.Join("&", args.Select(f => f.Key + "=" + HttpUtility.UrlEncode(f.Value)));
      }

      System.Diagnostics.Debug.WriteLine("GetAction: " + result);
      return result;
    }
  }
}