/*
 * Copyright 2015 Matt Cosand
 */
namespace Kcesar.MissionLine.Website.Controllers
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Web.Mvc;
  using System.Web.Routing;
  using Kcesar.MissionLine.Website.Data;
  using Twilio.TwiML;
  using Twilio.TwiML.Mvc;

  public class VoiceController : TwilioController
  {
    private string memberId = null;
    private string memberName = null;
    private bool hasRecording = false;
    private bool isSignedIn = false;

    private readonly IConfigSource config;
    private readonly IMemberSource members;
    private readonly Func<IMissionLineDbContext> dbFactory;

    public VoiceController()
      : this(() => new MissionLineDbContext(), new ConfigSource(), new MemberSource(new ConfigSource()))
    {
    }

    public VoiceController(Func<IMissionLineDbContext> dbFactory, IConfigSource config, IMemberSource members)
    {
      this.dbFactory = dbFactory;
      this.config = config;
      this.members = members;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="From"></param>
    /// <param name="CallSid"></param>
    /// <returns></returns>
    public TwiMLResult Answer(string From, string CallSid)
    {
      SetMemberInfoFromPhone(From);

      Db(db =>
      {
        var call = new VoiceCall
        {
          CallId = CallSid,
          Number = From,
          CallTime = GetLocalDateTime(),
          Name = memberName
        };

        db.Calls.Add(call);
        db.SaveChanges();
      });

      var response = BeginMenu();
      if (this.memberId != null)
      {
        response.SayVoice(Speeches.WelcomeUnknownCaller);
      }

      EndMenu(response);

      return TwiML(response);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="CallSid"></param>
    /// <param name="Digits"></param>
    /// <returns></returns>
    public TwiMLResult DoMenu(string CallSid, string Digits)
    {
      var response = new TwilioResponse();

      if (Digits == "1")
      {
        if (this.memberId == null)
        {
          AddLoginPrompt(response);
        }
        else
        {
          SignInOrOut(response, CallSid);
        }
      }
      else if (Digits == "2")
      {
        response.SayVoice(Speeches.StartRecording);
        response.Record(new { maxLength = 120, action = GetAction("StopRecording") });
        BeginMenu(response);
        EndMenu(response);
      }
      else if (Digits == "3")
      {
        AddLoginPrompt(response);
      }
      else
      {
        response.SayVoice("I didn't understand.");
        BeginMenu(response);
        EndMenu(response);
      }

      return TwiML(response);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Digits"></param>
    /// <returns></returns>
    public TwiMLResult DoLogin(string Digits)
    {
      string newMemberId = null;
      string newName = this.memberName;
      var response = new TwilioResponse();

      if (members.LookupMemberDEM(Digits, out newMemberId, out newName))
      {
        this.memberId = newMemberId;
        this.memberName = newName;
        BeginMenu(response);
        EndMenu(response);
      }
      else
      {
        AddLoginPrompt(response);
      }

      return TwiML(response);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Digits"></param>
    /// <returns></returns>
    public TwiMLResult SetMiles(string Digits)
    {
      int miles;
      if (int.TryParse(Digits, out miles))
      {
        Db(db =>
        {
          var call = db.SignIns.OrderByDescending(f => f.TimeIn).FirstOrDefault(f => f.MemberId == memberId);
          call.Miles = miles;
          db.SaveChanges();
        });
      }

      var response = BeginMenu();
      response.SayVoice("Your miles have been updated.");
      EndMenu(response, true);
      return TwiML(response);
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
      });
    }

    protected override void Initialize(RequestContext requestContext)
    {
      base.Initialize(requestContext);
      memberId = Request.QueryString["memberId"];
      memberName = Request.QueryString["memberName"];
      this.hasRecording = (Request.QueryString["hasR"] == "1");
      this.isSignedIn = (Request.QueryString["isS"] == "1");

      System.Diagnostics.Debug.WriteLine(Request.RawUrl);
      foreach (var key in Request.Form)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("{0}: {1}", key, Request.Form[(string)key]));
      }
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
      members.LookupMemberPhone(From, out this.memberId, out this.memberName);
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

    public TwiMLResult StopRecording(string CallSid, string RecordingUrl, int? RecordingDuration)
    {
      TwilioResponse response = null;
      Db(db =>
      {
        var call = db.Calls.Single(f => f.CallId == CallSid);
        //if (!string.IsNullOrWhiteSpace(call.RecordingUrl))
        //{
        //  // Delete previous recording
        //}
        call.RecordingDuration = RecordingDuration;
        call.RecordingUrl = RecordingUrl;
        db.SaveChanges();

        response = BeginMenu();
        response.SayVoice("Recording saved.");
        EndMenu(response, true);
      });
      return TwiML(response);
    }

    public TwiMLResult Complete(string CallSid, int? CallDuration)
    {
      Db(db =>
      {
        var call = db.Calls.Where(f => f.CallId == CallSid).Single();
        call.Duration = CallDuration;
        db.SaveChanges();
      });

      var response = new TwilioResponse();
      response.Hangup();
      return TwiML(response);
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

      string result = this.config.GetUrlAction(Url, name);
      if (args.Count > 0)
      {
        result += "?" + string.Join("&", args.Select(f => f.Key + "=" + Url.Encode(f.Value)));
      }

      System.Diagnostics.Debug.WriteLine("GetAction: " + result);
      return result;
    }
  }
}