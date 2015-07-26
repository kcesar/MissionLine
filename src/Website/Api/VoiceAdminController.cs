/*
 * Copyright 2015 Matt Cosand
 */
namespace Kcesar.MissionLine.Website.Api
{
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using System.Web.Http;
  using Data;
  using Model;
  using Services;
  using Twilio.TwiML;

  /// <summary>
  /// 
  /// </summary>
  [AllowAnonymous]
  [UseTwilioFormatter]
  public class VoiceAdminController : BaseVoiceController
  {
    /// <summary>
    /// 
    /// </summary>
    public VoiceAdminController()
      : this(() => new MissionLineDbContext(), new EventsService(() => new MissionLineDbContext(), new ConfigSource()), new ConfigSource())
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbFactory"></param>
    /// <param name="config"></param>
    private VoiceAdminController(Func<IMissionLineDbContext> dbFactory, IEventsService eventsService, IConfigSource config)
      :base (dbFactory, eventsService, config)
    {
    }
    
    [HttpPost]
    public async Task<TwilioResponse> Menu()
    {
      var response = new TwilioResponse();

      if (this.session.IsAdmin == false)
      {
        response.BeginGather(new { numDigits = 10, action = GetAction("Login", controller: "VoiceAdmin"), timeout = 10 });
        response.SayVoice("Enter admin password followed by pound");
        response.EndGather();
      }
      else
      {
        BeginMenu(response);
        await EndMenu(response);
      }

      return response;
    }

    [HttpPost]
    public async Task<TwilioResponse> Login(TwilioRequest request)
    {
      if (request.Digits == (this.config.GetConfig("AdminPassword") ?? "1954"))
      {
        this.session.IsAdmin = true;
      }
      return await Menu();
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
        var now = TimeUtils.GetLocalDateTime(this.config);
        var newEvent = new SarEvent { Name = "New Event at " + TimeUtils.GetMiltaryTimeVoiceText(now), Opened = now };
        await this.eventService.Create(newEvent);
        LoadActiveEvents();
        this.session.EventId = newEvent.Id;

        BeginMenu(response);
        response.SayVoice("Created " + newEvent.Name);
        
        await EndMenu(response);
      }
      else if (request.Digits == "2")
      {
        using (var db = dbFactory())
        {
          BuildSetEventMenu(response, string.Empty, Url.Content("~/api/VoiceAdmin/Menu"));
        }
      }
      else if (request.Digits == "3")
      {
        response.SayVoice("Record a short description of this event at the tone");
        response.Record(new { maxLength = 120, action = GetAction("SetDescription", controller: "VoiceAdmin") });
        BeginMenu(response);
        await EndMenu(response);
      }
      else if (request.Digits == "9")
      {
        Dictionary<string, string> args = new Dictionary<string, string>();
        args.Add("targetId", this.session.EventId.ToString());
        response.BeginGather(new { numDigits = 1, action = GetAction("ConfirmClose", args, controller: "VoiceAdmin"), timeout = 10 });
        response.SayVoice("Press 9 to confirm close of {0}. Press any other key to return to menu.", GetEventName());
        response.EndGather();
      }
      else if (request.Digits == "0")
      {
        response.Redirect(GetAction("Menu"));
      }
      else
      {
        response.SayVoice("I didn't understand.");
        BeginMenu(response);
        await EndMenu(response);
      }
      return response;
    }

    public async Task<TwilioResponse> ConfirmClose(TwilioRequest request)
    {
      var response = new TwilioResponse();
      int targetId;

      BeginMenu(response);
      if (int.TryParse(GetQueryParameter("targetId") ?? string.Empty, out targetId))
      {
        if (request.Digits != "9")
        {
          response.SayVoice("{0} was not closed.", GetEventName());
        }
        else
        {
          var target = await eventService.QuickClose(targetId);
          this.session.EventId = null;
          LoadActiveEvents();
          response.SayVoice("{0} was closed.", target.Name);
        }
      }
      await EndMenu(response);

      return response;
    }

    public async Task<TwilioResponse> SetDescription(TwilioRequest request)
    {
      await this.eventService.SetRecordedDescription(this.session.EventId.Value, request.RecordingUrl);

      TwilioResponse response = new TwilioResponse();
      BeginMenu(response);
      response.SayVoice("New greeting saved.");
      await EndMenu(response);

      return response;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="response"></param>
    private void BeginMenu(TwilioResponse response)
    {
      response.BeginGather(new { numDigits = 1, action = GetAction("DoMenu", controller: "VoiceAdmin"), timeout = 10 });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="response"></param>
    private async Task EndMenu(TwilioResponse response)
    {
      await StartMultiEventMenu(response, "create a new event");

      if (this.session.EventId != null)
      {
        response.SayVoice("Press 3 to record a new greeting");
        response.SayVoice("Press 9 to close this mission");
      }
      response.SayVoice("Press 0 to return to main menu");
      response.EndGather();
    }
  }
}