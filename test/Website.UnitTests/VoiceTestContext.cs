/*
 * Copyright 2015 Matthew Cosand
 */
namespace Website.UnitTests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net.Http;
  using System.Reflection;
  using System.Threading.Tasks;
  using System.Web.Http;
  using System.Web.Http.Controllers;
  using System.Web.Http.Hosting;
  using System.Xml.Linq;
  using Kcesar.MissionLine.Website;
  using Kcesar.MissionLine.Website.Api;
  using Kcesar.MissionLine.Website.Api.Model;
  using Kcesar.MissionLine.Website.Data;
  using Kcesar.MissionLine.Website.Services;
  using Moq;
  using NUnit.Framework;
  using Twilio.TwiML;

  public class VoiceTestContext : TestContext
  {
    public static readonly string AnswerUrl = "http://localhost/api/voice/answer";
    protected override void DefaultSetup()
    {
      base.DefaultSetup();

      CallSid = Guid.NewGuid().ToString() + DateTime.Now.ToString();
      From = "+11234567890";
      Member = new MemberLookupResult { Id = Guid.NewGuid().ToString(), Name = "Mr. Sandman" };

      MembersMock = new Mock<IMemberSource>();
      MembersMock.Setup(f => f.LookupMemberPhone(this.From)).Returns(() => Task.Factory.StartNew<MemberLookupResult>(() => this.Member));

      EventsServiceMock = new Mock<IEventsService>(MockBehavior.Strict);
      EventsServiceMock.Setup(f => f.ListActive()).Returns(() => Task.Factory.StartNew<List<SarEvent>>(() => new List<SarEvent>()));
    }

    public TwilioRequest CreateRequest(string digits)
    {
      return new TwilioRequest { CallSid = this.CallSid, From = this.From, Digits = digits ?? string.Empty };
    }

    public Mock<IMemberSource> MembersMock { get; private set; }

    public Mock<IEventsService> EventsServiceMock { get; private set; }

    public MemberLookupResult Member { get; set; }

    public string From { get; private set; }

    public string CallSid { get; private set; }

    public Task<TwilioResponse> DoApiCall(string url, string digits = null, bool redirects = true)
    {
      var args = this.CreateRequest(digits);
      return DoApiCall(url, args, redirects: redirects);
    }

    public async Task<TwilioResponse> DoApiCall(string url, TwilioRequest request, bool redirects = true)
    {
      HttpRequestMessage requestMessage;
      HttpControllerContext ctrlContext;
      MethodInfo method;
      GetActionMethod(url, out requestMessage, out ctrlContext, out method);

      var controller = new VoiceController(() => this.DBMock.Object, this.EventsServiceMock.Object, this.ConfigMock.Object, this.MembersMock.Object, new ConsoleLogger());
      controller.ControllerContext = ctrlContext;
      controller.RequestContext.Url = new System.Web.Http.Routing.UrlHelper(requestMessage);

      var queryArgs = ctrlContext.Request.GetQueryNameValuePairs().ToDictionary(f => f.Key, f => f.Value);
      controller.InitBody(queryArgs);

      var parameters = method.GetParameters();
      List<object> arguments = new List<object>();

      foreach (var parameter in parameters)
      {
        if (parameter.ParameterType == typeof(TwilioRequest)) { arguments.Add(request); }
        else if (parameter.ParameterType == typeof(string)) { arguments.Add(queryArgs[parameter.Name]); }

        else throw new NotImplementedException("Don't know how to bind parameter " + parameter.Name + " with type " + parameter.ParameterType.Name);
      }

      TwilioResponse result;
      if (method.ReturnType == typeof(TwilioResponse))
      {
        result = (TwilioResponse)method.Invoke(controller, arguments.ToArray());
      }
      else if (method.ReturnType == typeof(Task<TwilioResponse>))
      {
        result = await (Task<TwilioResponse>)method.Invoke(controller, arguments.ToArray());
      }
      else throw new NotImplementedException("API controller returns type " + method.ReturnType.Name);

      var first = result.ToXDocument().Root.FirstNode as XElement;
      if (redirects && first != null && first.Name == "Redirect")
      {
        result = await DoApiCall(first.Value, CreateRequest(null), true);
      }

      return result;
    }

    public MethodInfo GetActionMethod(string url)
    {
      HttpRequestMessage dummy1;
      HttpControllerContext dummy2;
      MethodInfo method;
      GetActionMethod(url, out dummy1, out dummy2, out method);
      return method;
    }

    private static void GetActionMethod(string url, out HttpRequestMessage requestMessage, out HttpControllerContext ctrlContext, out System.Reflection.MethodInfo method)
    {
      var config = new HttpConfiguration();
      WebApiConfig.Register(config);

      var controllerSelector = config.Services.GetHttpControllerSelector();
      var actionSelector = config.Services.GetActionSelector();

      requestMessage = new HttpRequestMessage(new HttpMethod("POST"), url);
      config.EnsureInitialized();

      var routeData = config.Routes.GetRouteData(requestMessage);
      requestMessage.Properties[HttpPropertyKeys.HttpRouteDataKey] = routeData;
      requestMessage.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;

      var ctrlDescriptor = controllerSelector.SelectController(requestMessage);
      ctrlContext = new HttpControllerContext(config, routeData, requestMessage)
      {
        ControllerDescriptor = ctrlDescriptor,
      };
      Assert.AreEqual(typeof(VoiceController), ctrlDescriptor.ControllerType);

      var actionDescriptor = (ReflectedHttpActionDescriptor)actionSelector.SelectAction(ctrlContext);
      method = actionDescriptor.MethodInfo;
    }
  }
}
