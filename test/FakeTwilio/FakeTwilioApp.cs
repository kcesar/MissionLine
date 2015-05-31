namespace FakeTwilio
{
  using System;
  using System.Collections.Generic;
  using System.Collections.Specialized;
  using System.Configuration;
  using System.Linq;
  using System.Net;
  using System.Text;
  using System.Xml.Linq;

  // Yes, yes... exception for flow control.
  public class HangupException : ApplicationException
  {
  }

  public class FakeTwilioApp
  {
    private string callSid;
    private string phoneNumber;
    private WebClient missionLine = new WebClient();
    private Uri baseAddress = new Uri((ConfigurationManager.AppSettings["MissionLineUrl"] ?? "http://localhost:47570/").TrimEnd('/') + "/api/voice/");

    static void Main(string[] args)
    {
      new FakeTwilioApp().Run();
    }

    private void Run()
    {
      this.callSid = Guid.NewGuid().ToString() + DateTime.Now.ToString();

      Console.Write("Caller's 10-digit phone #: ");
      phoneNumber = "+1" + Console.ReadLine();

      var startTime = DateTime.Now;
      var nextResult = Post("answer", new { CallSId = this.callSid, From = this.phoneNumber });
      try
      {
        while (nextResult != null)
        {
          nextResult = HandleResult(nextResult);
        }
      }
      catch (HangupException)
      {
        var duration = (int)(DateTime.Now - startTime).TotalSeconds;
        Post("Complete", new { CallSid = this.callSid, From = this.phoneNumber, CallDuration = duration });
      }
    }

    private List<string> scriptActionNames = new List<string> { "Gather", "Record" };

    private XDocument HandleResult(XDocument lastResult)
    {
      Console.WriteLine(new string('=', 40));
      while (true)
      {
        Console.WriteLine(lastResult);

        var tasks = lastResult.Descendants().Where(f => scriptActionNames.Contains(f.Name.ToString())).ToList();
        int taskIndex = 0;
        if (tasks.Count > 1)
        {
          taskIndex = tasks.Count;
          while (taskIndex >= tasks.Count)
          {
            taskIndex = ReadInteger("Which task? ") - 1;
          }
        }

        if (tasks[taskIndex].Name == "Gather")
        {
          Console.Write("Enter digits: ");
          var digits = Console.ReadLine();
          if (digits == "h") throw new HangupException();

          return Post(tasks[taskIndex].Attribute("action").Value, new { CallSid = this.callSid, From = this.phoneNumber, Digits = digits });
        }
        else if (tasks[taskIndex].Name == "Record")
        {
          Console.Write("Enter recording URL: ");
          var url = Console.ReadLine();
          if (url == "h") throw new HangupException();

          var duration = ReadInteger("Enter duration (s): ");

          return Post(tasks[taskIndex].Attribute("action").Value, new { CallSid = this.callSid, From = this.phoneNumber, RecordingUrl = url, RecordingDuration = duration });
        }
      }
    }

    private static int ReadInteger(string prompt)
    {
      int result;
      var input = string.Empty;
      while (int.TryParse(input, out result) == false)
      {
        Console.Write(prompt);
        input = Console.ReadLine();
        if (input == "h") throw new HangupException();
      }
      return result;
    }

    private XDocument Post(string url, object values)
    {
      values = values ?? new object();

      NameValueCollection formData = new NameValueCollection();
      foreach (var prop in values.GetType().GetProperties())
      {
        formData.Add(prop.Name, prop.GetValue(values).ToString());
      }

      var address = new Uri(this.baseAddress, url);
      string result = Encoding.UTF8.GetString(missionLine.UploadValues(address.AbsoluteUri, "POST", formData));

      return XDocument.Parse(result);
    }
  }
}
