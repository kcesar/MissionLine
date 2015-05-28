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
    bool LookupMemberPhone(string phone, out string newMemberId, out string newMemberName);
    bool LookupMemberDEM(string workerNumber, out string id, out string name);
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

    public bool LookupMemberPhone(string phone, out string newMemberId, out string newMemberName)
    {
      return DoLookup("/api/members/byphonenumber/" + phone.TrimStart('+'), out newMemberId, out newMemberName);
    }

    public bool LookupMemberDEM(string workerNumber, out string id, out string name)
    {
      return DoLookup("/api/members/byworkernumber/" + workerNumber, out id, out name);
    }

    private bool DoLookup(string url, out string newMemberId, out string newMemberName)
    {
      WebClient client = new WebClient() { Credentials = this.credential };
      MemberSummary[] members = JsonConvert.DeserializeObject<MemberSummary[]>(
        client.DownloadString(this.url + url + "?_auth=basic")
        );

      if (members.Length == 1)
      {
        newMemberId = members[0].Id.ToString();
        newMemberName = members[0].Name;
      }
      else
      {
        newMemberId = null;
        newMemberName = null;
      }

      return newMemberId != null;
    }
  }
}