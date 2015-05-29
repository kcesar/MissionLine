using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Kcesar.MissionLine.Website.Model;
using Newtonsoft.Json;

namespace Kcesar.MissionLine.Website
{
  public interface IMemberSource
  {
    MemberLookupResult LookupMemberPhone(string phone);
    MemberLookupResult LookupMemberDEM(string workerNumber);
  }

  public class MemberSource : IMemberSource
  {
    private readonly string url;
    private readonly NetworkCredential credential;

    public MemberSource(IConfigSource config)
    {
      this.url = config.GetConfig("databaseUrl").TrimEnd('/');
      this.credential = new NetworkCredential(config.GetConfig("databaseUsername"), config.GetConfig("databasePassword"));
    }

    public MemberLookupResult LookupMemberPhone(string phone)
    {
      return DoLookup("/api/members/byphonenumber/" + phone.TrimStart('+'));
    }

    public MemberLookupResult LookupMemberDEM(string workerNumber)
    {
      return DoLookup("/api/members/byworkernumber/" + workerNumber);
    }

    private MemberLookupResult DoLookup(string url)
    {
      WebClient client = new WebClient() { Credentials = this.credential };
      MemberSummary[] members = JsonConvert.DeserializeObject<MemberSummary[]>(
        client.DownloadString(this.url + url + "?_auth=basic")
        );

      if (members.Length == 1)
      {
        return new MemberLookupResult
        {
          Id = members[0].Id.ToString(),
          Name = members[0].Name
        };
      }

      return null;
    }
  }

  public class MemberLookupResult
  {
    public string Id { get; set; }
    public string Name { get; set; }
  }
}