/*
 * Copyright 2015 Matt Cosand
 */
namespace Kcesar.MissionLine.Website.Api
{
  using System;
  using System.Collections.Generic;
  using System.Data.Entity;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Web.Http;
  using Data;
  using log4net;
  using Model;
  using Services;
  using Twilio.TwiML;

  /// <summary>
  /// 
  /// </summary>
  [AllowAnonymous]
  [UseTwilioFormatter]
  public class VoiceController : BaseVoiceController
  {
    internal const string NextKey = "next";
    private readonly IMemberSource members;

    [HttpGet]
    public string Info()
    {
      string thisFile = new System.Uri(typeof(LogService).Assembly.CodeBase).LocalPath;
      string configFile = Path.Combine(Path.GetDirectoryName(thisFile), "..", "log4net.config");
      return configFile + ":" + File.Exists(configFile).ToString();
    }

    [HttpGet]
    public void Throw()
    {
      throw new NotImplementedException("blah");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbFactory"></param>
    /// <param name="config"></param>
    /// <param name="members"></param>
    public VoiceController(Func<IMissionLineDbContext> dbFactory, IEventsService eventService, IConfigSource config, IMemberSource members, ILog log)
      : base(dbFactory, eventService, config, log)
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

      var response = new TwilioResponse();
      BeginMenu(response);
      if (this.session.MemberId == null)
      {
        response.SayVoice(Speeches.WelcomeUnknownCaller);
      }

      await EndMenu(response);

      return LogResponse(response);
    }

    [HttpPost]
    public async Task<TwilioResponse> Menu(TwilioRequest request)
    {
      var response = new TwilioResponse();
      BeginMenu(response);
      await EndMenu(response);
      return LogResponse(response);
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
          await AddLoginPrompt(response, Url.Content("~/api/Voice/DoSignInOut"));
        }
        else
        {
          response = await DoSignInOut(request);
        }
      }
      else if (request.Digits == "3")
      {
        response.SayVoice(Speeches.StartRecording);
        response.Record(new { maxLength = 120, action = GetAction("StopRecording") });
        BeginMenu(response);
        await EndMenu(response);
      }
      else if (request.Digits == "8")
      {
        await AddLoginPrompt(response, Url.Content("~/api/voice/Menu"));
      }
      else if (request.Digits == "9")
      {
        response.Redirect(GetAction("Menu", controller: "VoiceAdmin"));
      }
      else
      {
        response.SayVoice(Speeches.InvalidSelection);
        BeginMenu(response);
        await EndMenu(response);
      }

      return LogResponse(response);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<TwilioResponse> DoSignInOut(TwilioRequest request)
    {
      var response = new TwilioResponse();

      using (var db = dbFactory())
      {
        var signin = await db.SignIns.Where(f => f.MemberId == this.session.MemberId).OrderByDescending(f => f.TimeIn).FirstOrDefaultAsync();
        var call = await db.Calls.SingleAsync(f => f.CallId == request.CallSid);

        DateTime time = TimeUtils.GetLocalDateTime(this.config);
        var sayDate = TimeUtils.GetMiltaryTimeVoiceText(time);

        if (signin == null || signin.TimeOut.HasValue)
        {
          if (this.session.IsSignedIn)
          {
            throw new InvalidOperationException("Tried to sign out when not signed in");
          }

          signin = new MemberSignIn
          {
            MemberId = this.session.MemberId,
            isMember = true,
            Name = this.session.MemberName,
            TimeIn = time,
            EventId = (this.CurrentEvents.Count == 1) ? this.CurrentEvents[0].Id : this.session.EventId,
          };

          db.SignIns.Add(signin);
          call.Actions.Add(new CallAction { Call = call, CallId = call.Id, Time = signin.TimeIn, Action = "Signed in " + signin.Name });
          await db.SaveChangesAsync();
          this.session.IsSignedIn = true;

          if (this.CurrentEvents.Count == 0)
          {
            BeginMenu(response);
            response.SayVoice(Speeches.SignedInUnassignedTemplate, this.session.MemberName, sayDate);
            await EndMenu(response);
          }
          else if (this.CurrentEvents.Count == 1)
          {
            signin.Event = this.CurrentEvents[0];
            BeginMenu(response);
            response.SayVoice(Speeches.SignedInTemplate, this.CurrentEvents[0].Name, this.session.MemberName, sayDate);
            await EndMenu(response);
          }
          else
          {
            BuildSetEventMenu(response, string.Format(Speeches.SignedInUnassignedTemplate, this.session.MemberName, sayDate), Url.Content("~/api/voice/SetSigninEvent"));
          }
        }
        else
        {
          signin.TimeOut = time;
          call.Actions.Add(new CallAction { Call = call, CallId = call.Id, Time = time, Action = "Signed out " + this.session.MemberName });
          this.session.IsSignedIn = false;
          await db.SaveChangesAsync();

          // add prompt for timeout beyond right now
          response.BeginGather(new { timeout = 10, action = GetAction("SetTimeOut") });
          response.SayVoice(Speeches.SignedOutTemplate, this.session.MemberName, sayDate);
          response.SayVoice(Speeches.TimeoutPrompt);
          response.EndGather();

        }
      }

      return LogResponse(response);
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
      var next = this.GetQueryParameter(NextKey);

      var lookup = await members.LookupMemberDEM(request.Digits);
      if (lookup != null)
      {
        this.session.MemberId = lookup.Id;
        this.session.MemberName = lookup.Name;
        await UpdateSigninStatus();

        response.Redirect((next ?? Url.Content("~/api/Voice/Menu")) + this.session.ToQueryString());
      }
      else
      {
        await AddLoginPrompt(response, next);
      }

      return LogResponse(response);
    }

    /// <summary>Records the time out for a member (they've already been signed out for the current time).</summary>
    /// <param name="request"></param>
    /// <returns>Prompt for miles</returns>
    [HttpPost]
    public async Task<TwilioResponse> SetTimeOut(TwilioRequest request)
    {
      int minutes;
      var response = new TwilioResponse();
      // add prompt for miles
      response.BeginGather(new { timeout = 10, action = GetAction("SetMiles") });
      if (int.TryParse(request.Digits, out minutes))
      {
        using (var db = dbFactory())
        {
          var signin = await GetMembersLatestSignin(db, this.session.MemberId);
          signin.TimeOut = signin.TimeOut.Value.AddMinutes(minutes);
          await db.SaveChangesAsync();
          this.config.GetPushHub<CallsHub>().updatedRoster(RosterController.GetRosterEntry(signin.Id, db));
          var sayDate = TimeUtils.GetMiltaryTimeVoiceText(signin.TimeOut.Value);
          response.SayVoice(Speeches.SignedOutTemplate, this.session.MemberName, sayDate);
        }
      }
      response.SayVoice(Speeches.MilesPrompt);
      response.EndGather();

      return LogResponse(response);
    }

    /// <summary>Records the miles driven for the period. Executed after sign out.</summary>
    /// <param name="request"></param>
    /// <returns>Main menu</returns>
    [HttpPost]
    public async Task<TwilioResponse> SetMiles(TwilioRequest request)
    {
      var response = new TwilioResponse();
      BeginMenu(response);

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
        response.SayVoice(Speeches.MilesUpdated);
      }

      await EndMenu(response, true);
      return LogResponse(response);
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
        call.RecordingUrl = request.RecordingUrl;
        await db.SaveChangesAsync();

        this.config.GetPushHub<CallsHub>().updatedCall(CallsController.GetCallEntry(call));

        BeginMenu(response);
        response.SayVoice(Speeches.CallerRecordingSaved);
        await EndMenu(response, true);
      }
      return LogResponse(response);
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
      return LogResponse(response);
    }

    // =========================================  END PUBLIC METHODS  =============================================

    private Task<MemberSignIn> GetMembersLatestSignin(IMissionLineDbContext db, string memberId)
    {
      return db.SignIns.OrderByDescending(f => f.TimeIn).FirstOrDefaultAsync(f => f.MemberId == memberId);
    }

    internal string GetSignInOutPrompt()
    {
      string sayAsMember = string.Format(this.session.MemberName != null ? Speeches.AsMemberTemplate : string.Empty, this.session.MemberName);
      return string.Format(this.session.IsSignedIn ? Speeches.PromptSignOutTemplate : Speeches.PromptSignInTemplate, sayAsMember);
    }

    private void BeginMenu(TwilioResponse response)
    {
      response.BeginGather(new { numDigits = 1, action = GetAction("DoMenu"), timeout = 10 });
    }

    private async Task EndMenu(TwilioResponse response, bool isContinuation = false)
    {
      await StartMultiEventMenu(response, GetSignInOutPrompt(), isContinuation);
      response.SayVoice(this.session.HasRecording ? Speeches.PromptRecordReplacementMessageTemplate : Speeches.PromptRecordMessageTemplate, 3);
      response.SayVoice(Speeches.PromptChangeResponder, 8);
      response.SayVoice(Speeches.PromptAdminMenu, 9);
      response.EndGather();
    }


    private async Task SetMemberInfoFromPhone(string From)
    {
      var lookup = await members.LookupMemberPhone(From);
      this.session.MemberId = lookup == null ? null : lookup.Id;
      this.session.MemberName = lookup == null ? null : lookup.Name;
    }

    private async Task AddLoginPrompt(TwilioResponse response, string next)
    {
      Dictionary<string, string> args = new Dictionary<string, string>();
      args.Add(NextKey, next ?? Url.Content("~/api/Voice/Menu"));

      response.BeginGather(new { timeout = 10, action = GetAction("DoLogin", args) });
      response.SayVoice(Speeches.DEMPrompt);
      response.SayVoice(Speeches.GoBack);
      response.EndGather();
      BeginMenu(response);
      await EndMenu(response);
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