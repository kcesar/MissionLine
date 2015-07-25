/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Api
{
  using System;
  using System.IO;
  using System.Net;
  using System.Net.Http;
  using System.Net.Http.Formatting;
  using System.Net.Http.Headers;
  using System.Threading.Tasks;
  using System.Web.Http.Controllers;
  using Twilio.TwiML;

  /// <summary>
  /// 
  /// </summary>
  public class TwilioResponseFormatter : MediaTypeFormatter
  {
    /// <summary>
    /// 
    /// </summary>
    public TwilioResponseFormatter()
    {
      SupportedMediaTypes.Clear();
      SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml"));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public override bool CanReadType(Type type)
    {
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public override bool CanWriteType(Type type)
    {
      return (type == typeof(TwilioResponse));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="headers"></param>
    /// <param name="mediaType"></param>
    public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
    {
      if (CanWriteType(type))
      {
        headers.ContentType = new MediaTypeHeaderValue("application/xml") { CharSet = "utf-8" };
      }
      base.SetDefaultContentHeaders(type, headers, mediaType);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <param name="writeStream"></param>
    /// <param name="content"></param>
    /// <param name="transportContext"></param>
    /// <returns></returns>
    public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
    {
      //return base.WriteToStreamAsync(type, value, writeStream, content, transportContext);
      return Task.Factory.StartNew(() =>
        {
          if (type == typeof(TwilioResponse))
          {
            var s = value.ToString();
            var b = System.Text.Encoding.UTF8.GetBytes(s);
            writeStream.Write(b, 0, b.Length);
          }
          else
          {
            base.WriteToStreamAsync(type, value, writeStream, content, transportContext);
          }
        });
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class UseTwilioFormatterAttribute : Attribute, IControllerConfiguration
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="controllerSettings"></param>
    /// <param name="controllerDescriptor"></param>
    public void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
    {
      //controllerSettings.Formatters.Clear();
      controllerSettings.Formatters.Insert(0, new TwilioResponseFormatter());
    }
  }

}