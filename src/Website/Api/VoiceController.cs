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
  using Services;

  /// <summary>
  /// 
  /// </summary>
  [AllowAnonymous]
  [UseTwilioFormatter]
  public class VoiceController : BaseVoiceController
  {
    internal static readonly string THEN_SIGNIN_KEY = "thenSignIn";

    private readonly IMemberSource members;

    /// <summary>
    /// 
    /// </summary>
    public VoiceController()
      : this(() => new MissionLineDbContext(), new ConfigSource())
    {
    }

    private VoiceController(Func<IMissionLineDbContext> dbFactory, IConfigSource config)
      : this(dbFactory, new EventsService(dbFactory, config), config, new MemberSource(config))
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbFactory"></param>
    /// <param name="config"></param>
    /// <param name="members"></param>
    public VoiceController(Func<IMissionLineDbContext> dbFactory, IEventsService eventService, IConfigSource config, IMemberSource members)
      : base(dbFactory, eventService, config)
    {
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
          CallTime = TimeUtils.GetLocalDateTime(this.config),
          Name = this.session.MemberName
        };

        db.Calls.Add(call);
        await db.SaveChangesAsync();

        this.config.GetPushHub<CallsHub>().updatedCall(CallsController.GetCallEntry(call));
      }

      var response = BeginMenu();
      if (this.session.MemberId == null)
      {
        response.SayVoice(Speeches.WelcomeUnknownCaller);
      }

      EndMenu(response);

      return response;
    }

    [HttpPost]
    public TwilioResponse Menu()
    {
      var response = new TwilioResponse();
      BeginMenu(response);
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
        if (this.session.MemberId == null)
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
      else if (request.Digits == "9")
      {
        response.Redirect(GetAction("Menu", controller: "VoiceAdmin"));
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
        this.session.MemberId = lookup.Id;
        this.session.MemberName = lookup.Name;
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
          var signin = db.SignIns.OrderByDescending(f => f.TimeIn).FirstOrDefault(f => f.MemberId == this.session.MemberId);
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
      string eventIdParameter = GetQueryParameter("evtIds");
      var nextUrl = GetQueryParameter("next");

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

      //using (var db = dbFactory())
      //{
      if (request.Digits.Length > 0)
      {
        int index;
        if (!int.TryParse(request.Digits, out index))
        {
          throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Digits not valid"));
        }

        this.session.EventId = eventIds[index - 1];
        response = new TwilioResponse();
        response.Redirect(nextUrl + this.session.ToQueryString());

        //if (index > 0 && index <= eventIds.Length)
        //{
        //  var signin = await db.SignIns.OrderByDescending(f => f.TimeIn).FirstOrDefaultAsync(f => f.MemberId == this.session.MemberId);
        //  signin.EventId = eventIds[index - 1];
        //  await db.SaveChangesAsync();

        //  this.config.GetPushHub<CallsHub>().updatedRoster(RosterController.GetRosterEntry(signin.Id, db));

        //  response = new TwilioResponse();
        //  BeginMenu(response);
        //  var theEvent = await db.Events.FirstOrDefaultAsync(f => f.Id == signin.EventId);
        //  response.SayVoice("You are now assigned to event {0}", theEvent.Name);
        //  EndMenu(response);
        //}


        if (response == null)
        {
          response = new TwilioResponse();
          using (var db = dbFactory())
          {
            BuildSetEventMenu(response, "I don't understand", EventsController.GetActiveEvents(db, this.config).Where(f => f.Closed == null).ToList());
          }
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
        DateTime time = TimeUtils.GetLocalDateTime(this.config);
        var sayDate = TimeUtils.GetMiltaryTimeVoiceText(time);


        // Get the last time the responder signed in or out.
        var signin = await db.SignIns.Where(f => f.MemberId == this.session.MemberId).OrderByDescending(f => f.TimeIn).FirstOrDefaultAsync();
        var call = await db.Calls.SingleAsync(f => f.CallId == callId);

        // If they've never signed in or have already signed out:
        if (signin == null || signin.TimeOut.HasValue)
        {
          if (this.session.IsSignedIn)
          {
            throw new InvalidOperationException("Tried to sign out when not signed in.");
          }

          var events = EventsController.GetActiveEvents(db, this.config).Where(f => f.Closed == null).ToList();

          // Sign them in.
          signin = new MemberSignIn
          {
            MemberId = this.session.MemberId,
            isMember = true,
            Name = this.session.MemberName,
            TimeIn = time
          };

          db.SignIns.Add(signin);
          call.Actions.Add(new CallAction { Call = call, CallId = call.Id, Time = time, Action = "Signed in " + this.session.MemberName });
          this.session.IsSignedIn = true;

          if (events.Count == 0)
          {
            BeginMenu(response);
            response.SayVoice(string.Format("Signed in as {0} at {1}", this.session.MemberName, sayDate));
            EndMenu(response);
          }
          else if (events.Count == 1)
          {
            signin.Event = events[0];
            BeginMenu(response);
            response.SayVoice(string.Format("Signed in to {0} as {1} at {2}", events[0].Name, this.session.MemberName, sayDate));
            EndMenu(response);
          }
          else
          {
            BuildSetEventMenu(response, string.Format("Signed in as {0} at {1}. ", this.session.MemberName, sayDate), events);
          }
        }
        else
        {
          signin.TimeOut = time;
          call.Actions.Add(new CallAction { Call = call, CallId = call.Id, Time = time, Action = "Signed out " + this.session.MemberName });
          this.session.IsSignedIn = false;
          // add prompt for miles
          response.BeginGather(new { timeout = 10, action = GetAction("SetMiles") });
          response.SayVoice("{0} signed out at {1}. Enter your miles followed by the pound key. Press pound if you did not drive.", this.session.MemberName, sayDate);
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
      string sayAsMember = this.session.MemberName != null ? (" as " + this.session.MemberName) : string.Empty;
      response.SayVoice("Press 1 to sign {0}{1}", this.session.IsSignedIn ? "out" : "in", sayAsMember);
      response.SayVoice(this.session.HasRecording ? "Press 2 to record a new message." : "Press 2 to record a message.");
      response.SayVoice("Press 3 to change current responder");
      response.SayVoice("Press 9 for admin options");
      response.EndGather();
    }

    private async Task SetMemberInfoFromPhone(string From)
    {
      var lookup = await members.LookupMemberPhone(From);
      this.session.MemberId = lookup == null ? null : lookup.Id;
      this.session.MemberName = lookup == null ? null : lookup.Name;
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

    private async Task UpdateSigninStatus()
    {
      if (string.IsNullOrWhiteSpace(this.session.MemberId))
      {
        return;
      }

      using (var db = dbFactory())
      {
        var signin = await db.SignIns.Where(f => f.MemberId == this.session.MemberId).OrderByDescending(f => f.TimeIn).FirstOrDefaultAsync();
        this.session.IsSignedIn = (signin != null) && (signin.TimeOut == null);
      };
    }
  }
}