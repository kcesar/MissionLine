/*
 * Copyright 2015 Matt Cosand
 */
namespace Kcesar.MissionLine.Website.Controllers
{
  using System;
  using System.Data.Entity;
  using System.Linq;
  using System.Web.Mvc;
  using Kcesar.MissionLine.Website.Data;
  using Twilio.TwiML;
  using Twilio.TwiML.Mvc;

  public class VoiceController : TwilioController
  {
    private string memberId = null;
    private string memberName = null;

    private readonly IConfigSource config;
    private readonly IMemberSource members;

    public VoiceController()
      : this(new ConfigSource(), new MemberSource(new ConfigSource()))
    {
    }

    public VoiceController(IConfigSource config, IMemberSource members)
    {
      this.config = config;
      this.members = members;
    }

    protected override void Initialize(System.Web.Routing.RequestContext requestContext)
    {
      base.Initialize(requestContext);
      memberId = Request.QueryString["memberId"];
      memberName = Request.QueryString["memberName"];
      System.Diagnostics.Debug.WriteLine(Request.RawUrl);
      foreach (var key in Request.Form)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("{0}: {1}", key, Request.Form[(string)key]));
      }
    }

    // GET: Voice
    public ActionResult Init(string From, string CallSid)
    {
      SetMemberInfoFromPhone(From);

      var response = new TwilioResponse();
      using (var db = new MissionLineDbContext())
      {
        AddMenu(response, r =>
        {
          response.Say(Speeches.Preamble);

          var evt = db.Events.FirstOrDefault();
          if (evt != null && !string.IsNullOrWhiteSpace(evt.OutgoingUrl))
          {
            response.Play(evt.OutgoingUrl);
          }
          else if (evt != null && !string.IsNullOrWhiteSpace(evt.OutgoingText))
          {
            response.Say(evt.OutgoingText);
          }
          else
          {
            response.Say(Speeches.DefaultOutgoing);
          }
        });

        var call = new VoiceCall
        {
          CallId = CallSid,
          Number = From,
          CallTime = GetLocalDateTime(),
          Name = memberName
        };

        db.Calls.Add(call);
        db.SaveChanges();
      }
      return TwiML(response);
    }

    private DateTime GetLocalDateTime()
    {
      return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, this.config.GetConfig("timezone") ?? "Pacific Standard Time");
    }

    public ActionResult Menu(string CallSid)
    {
      var response = new TwilioResponse();
      AddMenu(response);
      return TwiML(response);
    }

    public ActionResult DoMenu(string CallSid, string Digits)
    {
      var response = new TwilioResponse();

      switch (Digits[0])
      {
        case '1':
          AddSignInPrompt(response);
          break;
        case '2':
          response.Say(Speeches.StartRecording);
          response.Record(new { maxLength = 120, action = GetAction("StopRecording") });
          AddMenu(response);
          break;
        default:
          AddMenu(response);
          break;
      }

      return TwiML(response);
    }

    private void SetMemberInfoFromPhone(string From)
    {
      members.LookupMemberPhone(From, out memberId, out memberName);
    }



    private void AddMenu(TwilioResponse response)
    {
      AddMenu(response, null);
    }

    private void AddMenu(TwilioResponse response, Action<TwilioResponse> insertMessage)
    {
      response.BeginGather(new { numDigits = 1, action = GetAction("DoMenu"), timeout = 10 });
      if (insertMessage != null)
      {
        insertMessage(response);  
      }
      response.Say(Speeches.MenuBody);
      response.EndGather();
      response.Redirect(GetAction("Menu"));
    }

    private void AddLoginPrompt(TwilioResponse response)
    {
      response.BeginGather(new { timeout = 10, action = GetAction("DoLogin") });
      response.Say("Enter your D E M number followed by the pound key.");
      response.Say("To go back, press pound");
      response.EndGather();
      AddMenu(response);
    }

    public ActionResult DoLogin(string CallSid, string Digits)
    {
      string newMemberId = null;
      string newName = memberName;
      var response = new TwilioResponse();

      if (members.LookupMemberDEM(Digits, out newMemberId, out newName))
      {
        memberId = newMemberId;
        memberName = newName;
        AddSignInPrompt(response);
      }
      else
      {
        AddLoginPrompt(response);
      }

      return TwiML(response);
    }

    private void AddSignInPrompt(TwilioResponse response)
    {
      response.BeginGather(new { timeout = 10, numDigits = 1, action = GetAction("DoSignIn") });
      response.Say("Press 1 to sign in as " + memberName);
      response.Say("Press 2 to sign out.");
      response.Say("Press 3 to change user.");
      response.Say("Press pound for main menu.");
      response.EndGather();
      AddMenu(response);
    }

    public ActionResult DoSignIn(string CallSid, string Digits)
    {
      using (var db = new MissionLineDbContext())
      {
        var call = db.Calls.Where(f => f.CallId == CallSid).Single();

        var response = new TwilioResponse();
        DateTime time = GetLocalDateTime();
        string minuteText = time.ToString("m");
        if (time.Minute == 0)
        {
          minuteText = "hundred ";
        }
        else if (time.Minute < 10)
        {
          minuteText = "oh " + minuteText;
        }
        string dateText = time.ToString("H ") + minuteText;
        MemberSignIn signin = null;
        switch (Digits[0])
        {
          case '1':
            signin = db.SignIns.OrderByDescending(f => f.TimeIn).FirstOrDefault(f => f.MemberId == memberId && f.TimeOut == null);
            if (signin == null)
            {
              signin = new MemberSignIn { MemberId = memberId, Name = memberName };
              db.SignIns.Add(signin);
            }
            signin.TimeIn = time;
            db.SaveChanges();
            response.Say(string.Format("Signed in as {0} at {1}", memberName, dateText));
            AddMenu(response);
            break;
          case '2':
            signin = db.SignIns.OrderByDescending(f => f.TimeIn).FirstOrDefault(f => f.MemberId == memberId);
            if (signin == null)
            {
              response.Say("Can't sign out if you're not signed in");
              AddMenu(response);
            }
            else
            {
              signin.TimeOut = time;
              db.SaveChanges();
              response.BeginGather(new { timeout = 10, action = GetAction("SetMiles") });
              response.Say("You are signed out. Enter your miles followed by the pound key or push pound for menu");
              response.EndGather();
              AddMenu(response);
            }
            break;
          case '3':
            AddLoginPrompt(response);
            break;
          default:
            AddSignInPrompt(response);
            break;
        }

        return TwiML(response);
      }
    }

    public ActionResult SetMiles(string CallSid, string Digits)
    {
      int miles;
      if (int.TryParse(Digits, out miles))
      {
        using (var db = new MissionLineDbContext())
        {
          var call = db.SignIns.OrderByDescending(f => f.TimeIn).FirstOrDefault(f => f.MemberId == memberId);
          call.Miles = miles;
          db.SaveChanges();
        }
      }

      var response = new TwilioResponse();
      AddMenu(response);
      return TwiML(response);
    }

    private VoiceCall GetCallNoTracking(string callSid)
    {
      using (var db = new MissionLineDbContext())
      {
        return db.Calls.AsNoTracking().Where(f => f.CallId == callSid).Single();
      }      
    }

    public ActionResult StopRecording(string CallSid, string RecordingUrl, int? RecordingDuration)
    {
      using (var db = new MissionLineDbContext())
      {
        var call = db.Calls.Single(f => f.CallId == CallSid);
        //if (!string.IsNullOrWhiteSpace(call.RecordingUrl))
        //{
        //  // Delete previous recording
        //}
        call.RecordingDuration = RecordingDuration;
        call.RecordingUrl = RecordingUrl;
        db.SaveChanges();
        
        var response = new TwilioResponse();
        AddMenu(response);
        return TwiML(response);
      }
    }

    public ActionResult Complete(string CallSid, int? CallDuration)
    {
      using (var db = new MissionLineDbContext())
      {
        var call = db.Calls.Where(f => f.CallId == CallSid).Single();
        call.Duration = CallDuration;
        db.SaveChanges();
      }

      var response = new TwilioResponse();
      response.Say(Speeches.Bye);
      response.Hangup();
      return TwiML(response);
    }

    private string GetAction(string name)
    {
      string query = memberId == null
        ? string.Empty
        : ("?memberId=" + memberId + "&memberName=" + Url.Encode(memberName));
      string result = Url.Action(name) + query;

      System.Diagnostics.Debug.WriteLine("GetAction: " + result);
      return result;
    }
  }
}