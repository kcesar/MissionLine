/*
 * Copyright 2015 Matt Cosand
 */
namespace Kcesar.MissionLine.Website.Api
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net.Http;
  using System.Text.RegularExpressions;
  using System.Threading.Tasks;
  using System.Web;
  using System.Web.Http;
  using Data;
  using Services;
  using Twilio.TwiML;

  /// <summary>
  /// 
  /// </summary>
  [AllowAnonymous]
  [UseTwilioFormatter]
  public abstract class BaseVoiceController : ApiController
  {
    // public for testing.
    public readonly QueryFields session = new QueryFields();

    protected readonly IConfigSource config;
    protected readonly Func<IMissionLineDbContext> dbFactory;
    protected readonly IEventsService eventService;
    protected List<SarEvent> CurrentEvents { get; set; }

    protected static Regex urlReplace = new Regex("^https?\\:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    protected bool EventWasChanged { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public BaseVoiceController()
      : this(() => new MissionLineDbContext(), new EventsService(() => new MissionLineDbContext(), new ConfigSource()), new ConfigSource())
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbFactory"></param>
    /// <param name="config"></param>
    public BaseVoiceController(Func<IMissionLineDbContext> dbFactory, IEventsService eventService, IConfigSource config)
    {
      this.dbFactory = dbFactory;
      this.config = config;
      this.eventService = eventService;
    }

    protected override void Initialize(System.Web.Http.Controllers.HttpControllerContext controllerContext)
    {
      base.Initialize(controllerContext);
      var queryArgs = controllerContext.Request.GetQueryNameValuePairs();
      InitBody(queryArgs);
    }

    internal void InitBody(IEnumerable<KeyValuePair<string, string>> queryArgs)
    {
      this.EventWasChanged = false;
      this.session.Load(queryArgs);
      LoadActiveEvents();
    }

    protected void LoadActiveEvents()
    {
      this.CurrentEvents = Task.Run(() => eventService.ListActive()).Result;
      if (this.CurrentEvents.Count == 1 && this.session.EventId == null)
      {
        this.session.EventId = this.CurrentEvents[0].Id;
        this.EventWasChanged = true;
      }
    }

    protected void BuildSetEventMenu(TwilioResponse response, string prompt, string thenUrl)
    {
      Dictionary<string, string> args = new Dictionary<string, string>();
      args.Add("evtIds", string.Join(".", this.CurrentEvents.Select(f => f.Id.ToString())));
      args.Add("next", thenUrl);

      response.BeginGather(new { timeout = 10, action = GetAction("SetEvent", args) });
      if (!string.IsNullOrWhiteSpace(prompt))
      {
        response.SayVoice(prompt);
      }
      response.SayVoice(string.Format("There are {0} events in progress. ", this.CurrentEvents.Count));
      for (int i = 0; i < this.CurrentEvents.Count; i++)
      {
        response.SayVoice(string.Format("Press {0} then pound for {1}. ", i + 1, this.CurrentEvents[i].Name));
      }
      response.EndGather();
    }

    protected string GetAction(string name, Dictionary<string, string> args = null, string controller = "voice")
    {
      string result = this.Url.Content(string.Format("~/api/{0}/{1}", controller, name)) + this.session.ToQueryString(args);

      System.Diagnostics.Debug.WriteLine("GetAction: " + result);
      return result;
    }

    protected string GetQueryParameter(string key)
    {
      return this.Request.GetQueryNameValuePairs()
                          .Where(f => f.Key == key)
                          .Select(f => f.Value)
                          .FirstOrDefault();
    }

    protected string GetEventName()
    {
      return this.CurrentEvents.Where(f => f.Id == this.session.EventId).Select(f => f.Name).SingleOrDefault();
    }

    protected async Task<List<SarEvent>> StartMultiEventMenu(TwilioResponse response, string pressOnePrompt, bool isContinuation = false)
    {
      var activeEvents = await this.eventService.ListActive();
      if (activeEvents.Count > 1)
      {
        response.SayVoice(Speeches.ActiveEventsTemplate, activeEvents.Count);
      }
      if (this.session.EventId != null)
      {
        var theEvent = activeEvents.Where(f => f.Id == this.session.EventId).SingleOrDefault();
        response.SayVoice(Speeches.CurrentEventTemplate, theEvent == null ? Speeches.UnknownEvent : theEvent.Name);
        
        if (this.EventWasChanged && theEvent != null)
        {
          if (!string.IsNullOrWhiteSpace(theEvent.OutgoingUrl))
          {
            response.Play(theEvent.OutgoingUrl);
          }
          else if (!string.IsNullOrWhiteSpace(theEvent.OutgoingText))
          {
            response.SayVoice(theEvent.OutgoingText);
          }
        }
      }
      if (isContinuation)
      {
        response.SayVoice(Speeches.HangUpOr);
      }
      response.SayVoice(Speeches.PressOneTemplate, pressOnePrompt);
      if (activeEvents.Count > 1)
      {
        response.SayVoice(Speeches.PromptSwitchEventTemplate, 2);
      }
      return activeEvents;
    }

    public class QueryFields
    {
      internal const string SignedInKey = "s";

      public string MemberId { get; set; }
      public string MemberName { get; set; }
      public bool HasRecording { get; set; }
      public bool IsSignedIn { get; set; }
      public int? EventId { get; set; }
      public bool IsAdmin { get; set; }
      public void Load(IEnumerable<KeyValuePair<string, string>> queries)
      {
        string eventId = queries.Where(f => f.Key == "e").Select(f => f.Value).FirstOrDefault();

        MemberId = queries.Where(f => f.Key == "m").Select(f => f.Value).FirstOrDefault();
        MemberName = queries.Where(f => f.Key == "n").Select(f => f.Value).FirstOrDefault();
        HasRecording = queries.Where(f => f.Key == "r").Select(f => f.Value).FirstOrDefault() == "1";
        IsSignedIn = queries.Where(f => f.Key == SignedInKey).Select(f => f.Value).FirstOrDefault() == "1";
        IsAdmin = queries.Where(f => f.Key == "a").Select(f => f.Value).FirstOrDefault() == "1";
        EventId = eventId == null ? (int?)null : int.Parse(eventId);        
      }

      public string ToQueryString(IDictionary<string, string> additionalArgs = null)
      {
        Dictionary<string, string> fields = additionalArgs == null ? new Dictionary<string, string>() : new Dictionary<string, string>(additionalArgs);

        if (!string.IsNullOrWhiteSpace(this.MemberId))
        {
          fields.Add("m", HttpUtility.UrlEncode(this.MemberId));
          fields.Add("n", HttpUtility.UrlEncode(this.MemberName));
        }
        if (this.EventId.HasValue)
        {
          fields.Add("e", this.EventId.Value.ToString());
        }
        if (this.HasRecording)
        {
          fields.Add("r", "1");
        }
        if (this.IsSignedIn)
        {
          fields.Add(SignedInKey, "1");
        }
        if (this.IsAdmin)
        {
          fields.Add("a", "1");
        }

        return fields.Count == 0 ? string.Empty : ("?" + string.Join("&", fields.Select(f => f.Key + "=" + f.Value)));
      }
    }
  }
}