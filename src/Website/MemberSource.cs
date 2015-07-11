namespace Kcesar.MissionLine.Website
{
  using System;
  using System.Net;
  using System.Threading.Tasks;
  using Kcesar.MissionLine.Website.Model;
  using Newtonsoft.Json;
  
  public interface IMemberSource
  {
    Task<MemberLookupResult> LookupMemberPhone(string phone);
    Task<MemberLookupResult> LookupMemberDEM(string workerNumber);
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

    public Task<MemberLookupResult> LookupMemberPhone(string phone)
    {
      return DoLookup("/api/members/byphonenumber/" + phone.TrimStart('+'));
    }

    public Task<MemberLookupResult> LookupMemberDEM(string workerNumber)
    {
      return DoLookup("/api/members/byworkernumber/" + workerNumber);
    }

    private async Task<MemberLookupResult> DoLookup(string url)
    {
      WebClient client = new WebClient() { Credentials = this.credential };
      MemberSummary[] members = JsonConvert.DeserializeObject<MemberSummary[]>(
        await client.DownloadStringTaskAsync(new Uri(this.url + url + "?_auth=basic"))
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