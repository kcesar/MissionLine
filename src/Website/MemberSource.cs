/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System;
  using System.Net;
  using System.Net.Http;
  using System.Net.Http.Headers;
  using System.Runtime.Caching;
  using System.Threading.Tasks;
  using IdentityModel.Client;
  using Kcesar.MissionLine.Website.Model;
  using log4net;
  using Newtonsoft.Json;

  public interface IMemberSource
  {
    Task<MemberLookupResult> LookupMemberPhone(string phone);
    Task<MemberLookupResult> LookupMemberDEM(string workerNumber);
    //  Task<MemberLookupResult> LookupMemberUsername(string username);
  }

  public class MemberSource : IMemberSource
  {
    private readonly string _url;
    private readonly string _authorityUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private string token;
    private DateTime tokenExpiry = DateTime.MinValue;
    private readonly object tokenLock = new object();
    //  private readonly NetworkCredential credential;
    private readonly ILog log;

    //private static readonly MemoryCache usersCache = new MemoryCache("usernames");
    //private static readonly object cacheLock = new object();
    //private static readonly CacheItemPolicy cachePolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(10) };
    private static readonly MemberLookupResult nullUser = new MemberLookupResult();

    public MemberSource(IConfigSource config, ILog log)
    {
      _authorityUrl = config.GetConfig("auth:authority");
      _url = config.GetConfig("api:root").TrimEnd('/');
      _clientId = config.GetConfig("api:clientId");
      _clientSecret = config.GetConfig("api:secret");
      this.log = log;
    }

    public Task<MemberLookupResult> LookupMemberPhone(string phone)
    {
      return DoLookup("/members/byphonenumber/" + phone.TrimStart('+'));
    }

    public Task<MemberLookupResult> LookupMemberDEM(string workerNumber)
    {
      return DoLookup("/members/byworkernumber/" + workerNumber);
    }

    //public async Task<MemberLookupResult> LookupMemberUsername(string username)
    //{
    //  MemberLookupResult result;
    //  lock(cacheLock)
    //  {
    //    result = (MemberLookupResult)usersCache.Get(username);
    //  }

    //  if (result == null)
    //  {
    //    result = await DoLookup("/api/members/byusername/" + Uri.EscapeUriString(username)) ?? nullUser;        
    //    lock (cacheLock)
    //    {
    //      usersCache.Set(username, result, cachePolicy);
    //    }
    //  }
    //  return result == nullUser ? null : result;
    //}

    private async Task<string> GetToken()
    {
      if (tokenExpiry < DateTime.Now)
      {
        var tokenClient = new TokenClient(
          _authorityUrl + "/connect/token",
          _clientId,
          _clientSecret);
        var response = await tokenClient.RequestClientCredentialsAsync("database-api db-r-members");
        lock (tokenLock)
        {
          token = response.AccessToken;
          tokenExpiry = DateTime.Now.AddSeconds(response.ExpiresIn - 100);
        }
      }
      return token;
    }

    private async Task<MemberLookupResult> DoLookup(string url)
    {
      var token = await GetToken();
      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

      url = _url + url;
      string response;
      try
      {
        response = await client.GetStringAsync(url);
      }
      catch (Exception ex)
      {
        this.log.Error("While querying " + url, ex);
        throw;
      }

      MemberSummary[] members = JsonConvert.DeserializeObject<MemberSummary[]>(response);

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