/*
 * Copyright 2015 Matt Cosand
 */
namespace Kcesar.MissionLine.Website.Api.Controllers
{
  using System;
  using System.Collections.Generic;
  using System.Data.Entity;
  using System.Linq;
  using System.Net.Http;
  using System.Text.RegularExpressions;
  using System.Threading.Tasks;
  using System.Web;
  using System.Web.Http;
  using Model;
  using Data;
  using Twilio.TwiML;
  using System.Net;


  /// <summary>
  /// 
  /// </summary>
  [UseTwilioFormatter]
  public class VoiceController : ApiController
  {
    internal static readonly string THEN_SIGNIN_KEY = "thenSignIn";

    private string memberId = null;
    private string memberName = null;
    private bool hasRecording = false;
    private bool isSignedIn = false;

    private readonly IConfigSource config;
    private readonly IMemberSource members;
    private readonly Func<IMissionLineDbContext> dbFactory;

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
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<TwilioResponse> Answer(TwilioRequest request)
    {
      await SetMemberInfoFromPhone(request.From);
      await UpdateSigninStatus();

      using (var db = dbFactory())
      {
        var call = new VoiceCall
        {
          CallId = request.CallSid,
          Number = request.From,
          CallTime = GetLocalDateTime(),
          Name = memberName
        };

        db.Calls.Add(call);
        await db.SaveChangesAsync();

        this.config.GetPushHub<CallsHub>().updatedCall(CallsController.GetCallEntry(call));
      }

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
    public async Task<TwilioResponse> DoMenu(TwilioRequest request)
    {
      var response = new TwilioResponse();

      if (request.Digits == "1")
      {
        if (this.memberId == null)
        {
          AddLoginPrompt(response, true);
        }
        else
        {
          await SignInOrOut(response, request.CallSid);
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
    public async Task<TwilioResponse> DoLogin(TwilioRequest request)
    {
      var response = new TwilioResponse();

      var lookup = await members.LookupMemberDEM(request.Digits);
      if (lookup != null)
      {
        this.memberId = lookup.Id;
        this.memberName = lookup.Name;
        await UpdateSigninStatus();
        var thenSigninParameter = this.Request.GetQueryNameValuePairs()
                    .Where(f => f.Key == THEN_SIGNIN_KEY)
                    .Select(f => f.Value)
                    .FirstOrDefault();
        if (thenSigninParameter != null)
        {
          await SignInOrOut(response, request.CallSid);
        }
        else
        {
          BeginMenu(response);
          EndMenu(response);
        }
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
    public async Task<TwilioResponse> SetMiles(TwilioRequest request)
    {
      int miles;
      if (int.TryParse(request.Digits, out miles))
      {
        using (var db = dbFactory())
        {
          var signin = db.SignIns.OrderByDescending(f => f.TimeIn).FirstOrDefault(f => f.MemberId == memberId);
          signin.Miles = miles;
          await db.SaveChangesAsync();
          this.config.GetPushHub<CallsHub>().updatedRoster(RosterController.GetRosterEntry(signin.Id, db));
        }
      }

      var response = BeginMenu();
      response.SayVoice("Your miles have been updated.");
      EndMenu(response, true);
      return response;
    }

    [HttpPost]
    public async Task<TwilioResponse> SetEvent(TwilioRequest request)
    {
      var eventIdParameter = this.Request.GetQueryNameValuePairs()
                          .Where(f => f.Key == "evtIds")
                          .Select(f => f.Value)
                          .FirstOrDefault();
      if (eventIdParameter == null)
      {
        throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "evtIds not included in query"));
      }

      // get the list of event ids as they were when the user got the menu
      var eventIds = eventIdParameter
                        .Split('.')
                        .Select(f => { int id; if (!int.TryParse(f, out id)) { throw new HttpResponseException(HttpStatusCode.BadRequest); } return id; })
                        .ToArray();

      TwilioResponse response = null;
      
      using (var db = dbFactory())
      {
        if (request.Digits.Length > 0)
        {
          int index;
          if (!int.TryParse(request.Digits, out index))
          {
            throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Digits not valid"));
          }

          if (index > 0 && index <= eventIds.Length)
          {
            var signin = await db.SignIns.OrderByDescending(f => f.TimeIn).FirstOrDefaultAsync(f => f.MemberId == memberId);
            signin.EventId = eventIds[index - 1];
            await db.SaveChangesAsync();

            this.config.GetPushHub<CallsHub>().updatedRoster(RosterController.GetRosterEntry(signin.Id, db));

            response = new TwilioResponse();
            BeginMenu(response);
            var theEvent = await db.Events.FirstOrDefaultAsync(f => f.Id == signin.EventId);
            response.SayVoice("You are now assigned to event {0}", theEvent.Name);
            EndMenu(response);
          }
        }

        if (response == null)
        {
          response = new TwilioResponse();
          BuildSetEventMenu(response, "I don't understand", EventsController.GetActiveEvents(db, this.config).Where(f => f.Closed == null).ToList());
        }
      }

      return response;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<TwilioResponse> StopRecording(TwilioRequest request)
    {
      TwilioResponse response = null;
      using (var db = dbFactory())
      {
        var call = db.Calls.Single(f => f.CallId == request.CallSid);
        //if (!string.IsNullOrWhiteSpace(call.RecordingUrl))
        //{
        //  // Delete previous recording
        //}
        call.RecordingDuration = request.RecordingDuration;
        call.RecordingUrl = urlReplace.Replace(request.RecordingUrl, string.Empty);
        await db.SaveChangesAsync();

        this.config.GetPushHub<CallsHub>().updatedCall(CallsController.GetCallEntry(call));

        response = BeginMenu();
        response.SayVoice("Recording saved.");
        EndMenu(response, true);
      }
      return response;
    }
    private static Regex urlReplace = new Regex("^https?\\:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<TwilioResponse> Complete(TwilioRequest request)
    {
      using (var db = dbFactory())
      {
        var call = db.Calls.Where(f => f.CallId == request.CallSid).SingleOrDefault();
        if (call != null)
        {
          call.Duration = request.CallDuration;
          await db.SaveChangesAsync();

          this.config.GetPushHub<CallsHub>().updatedCall(CallsController.GetCallEntry(call));
        }
      };

      var response = new TwilioResponse();
      response.Hangup();
      return response;
    }

    // =========================================  END PUBLIC METHODS  =============================================


    private async Task SignInOrOut(TwilioResponse response, string callId)
    {
      using (var db = dbFactory())
      {
        DateTime time = GetLocalDateTime();
        var sayDate = GetMiltaryTimeVoiceText(time);


        // Get the last time the responder signed in or out.
        var signin = await db.SignIns.Where(f => f.MemberId == this.memberId).OrderByDescending(f => f.TimeIn).FirstOrDefaultAsync();
        var call = await db.Calls.SingleAsync(f => f.CallId == callId);

        // If they've never signed in or have already signed out:
        if (signin == null || signin.TimeOut.HasValue)
        {
          if (this.isSignedIn)
          {
            throw new InvalidOperationException("Tried to sign out when not signed in.");
          }

          var events = EventsController.GetActiveEvents(db, this.config).Where(f => f.Closed == null).ToList();

          // Sign them in.
          signin = new MemberSignIn
          {
            MemberId = this.memberId,
            isMember = true,
            Name = this.memberName,
            TimeIn = time
          };

          db.SignIns.Add(signin);
          call.Actions.Add(new CallAction { Call = call, CallId = call.Id, Time = time, Action = "Signed in " + this.memberName });
          this.isSignedIn = true;

          if (events.Count == 0)
          {
            BeginMenu(response);
            response.SayVoice(string.Format("Signed in as {0} at {1}", this.memberName, sayDate));
            EndMenu(response);
          }
          else if (events.Count == 1)
          {
            signin.Event = events[0];
            BeginMenu(response);
            response.SayVoice(string.Format("Signed in to {0} as {1} at {2}", events[0].Name, this.memberName, sayDate));
            EndMenu(response);
          }
          else
          {
            BuildSetEventMenu(response, string.Format("Signed in as {0} at {1}. ", this.memberName, sayDate), events);
          }
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
        await db.SaveChangesAsync();
        var hub = this.config.GetPushHub<CallsHub>();
        hub.updatedRoster(RosterController.GetRosterEntry(signin.Id, db));
        hub.updatedCall(CallsController.GetCallEntry(call));
      }
    }

    private void BuildSetEventMenu(TwilioResponse response, string prompt, List<SarEvent> events)
    {
      Dictionary<string, string> args = new Dictionary<string, string>();
      args.Add("evtIds", string.Join(".", events.Select(f => f.Id.ToString())));

      response.BeginGather(new { timeout = 10, action = GetAction("SetEvent", args) });
      response.SayVoice(prompt);
      response.SayVoice(string.Format("There are {0} events in progress. ", events.Count));
      // response.SayVoice("Enter the 4 digit D E M number or ");
      for (int i = 0; i < events.Count; i++)
      {
        response.SayVoice(string.Format("Press {0} then pound for {1}. ", i, events[i].Name));
      }
      response.EndGather();
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

    private DateTime GetLocalDateTime()
    {
      return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, this.config.GetConfig("timezone") ?? "Pacific Standard Time");
    }


    private static string GetMiltaryTimeVoiceText(DateTime time)
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

    private async Task SetMemberInfoFromPhone(string From)
    {
      var lookup = await members.LookupMemberPhone(From);
      this.memberId = lookup == null ? null : lookup.Id;
      this.memberName = lookup == null ? null : lookup.Name;
    }

    private void AddLoginPrompt(TwilioResponse response, bool thenSignin = false)
    {
      Dictionary<string, string> args = new Dictionary<string, string>();
      if (thenSignin)
      {
        args.Add(THEN_SIGNIN_KEY, "yes");
      }

      response.BeginGather(new { timeout = 10, action = GetAction("DoLogin", args) });
      response.SayVoice("Enter your D E M number followed by the pound key.");
      response.SayVoice("To go back, press pound");
      response.EndGather();
      BeginMenu(response);
      EndMenu(response);
    }

    private string GetAction(string name, Dictionary<string, string> args = null)
    {
      args = args ?? new Dictionary<string, string>();
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

    private async Task UpdateSigninStatus()
    {
      if (string.IsNullOrWhiteSpace(this.memberId))
      {
        return;
      }

      using (var db = dbFactory())
      {
        var signin = await db.SignIns.Where(f => f.MemberId == this.memberId).OrderByDescending(f => f.TimeIn).FirstOrDefaultAsync();
        this.isSignedIn = (signin != null) && (signin.TimeOut == null);
      };
    }

  }
}