/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System;
  using System.Collections.Generic;
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
    Task<List<MemberLookupResult>> TryLookupMembersAsync(IEnumerable<string> memberIds);
    Task<List<PersonContact>> LookupPersonContactsAsync(string memberId);
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
      _authorityUrl = config.GetConfig("api:authority");
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

    /// <summary>
    /// Attempts to find the specified members. If not found, or other failures occur, the failed members are skipped.
    /// </summary>
    /// <param name="memberIds"></param>
    /// <returns></returns>
    public async Task<List<MemberLookupResult>> TryLookupMembersAsync(IEnumerable<string> memberIds)
    {
      // TODO: In future, KCSARA-Database should be updated to have an API to look up multiple members.
      // Right now it can just look up one at a time.
      // https://github.com/mcosand/KCSARA-Database/blob/13d5d5a9d94203c7fc577eb0cef99c45d5cee487/src/database-api/Controllers/Members/MembersController.cs

      List<MemberLookupResult> answer = new List<MemberLookupResult>();
      foreach (var memberId in memberIds)
      {
        try
        {
          var member = await DoLookup("/members/" + memberId, returnsArray: false);
          if (member != null)
          {
            answer.Add(member);
          }
        }
        catch { }
      }
      return answer;
    }

    public async Task<List<PersonContact>> LookupPersonContactsAsync(string memberId)
    {
      return await DoLookupAsync<List<PersonContact>>("/members/" + memberId + "/contacts");
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

    private async Task<MemberLookupResult> DoLookup(string url, bool returnsArray = true)
    {
      MemberSummary memberSummary = null;

      // Most of the time the APIs return an array
      if (returnsArray)
      {
        MemberSummary[] members = await DoLookupAsync<MemberSummary[]>(url);
        if (members.Length == 1)
        {
          memberSummary = members[0];
        }
      }

      // However, some just return a single object
      else
      {
        memberSummary = await DoLookupAsync<MemberSummary>(url);
      }

      if (memberSummary != null)
      {
        return new MemberLookupResult
        {
          Id = memberSummary.Id.ToString(),
          Name = memberSummary.Name
        };
      }

      return null;
    }

    private async Task<T> DoLookupAsync<T>(string url)
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

      return JsonConvert.DeserializeObject<T>(response);
    }
  }

  public class MemberLookupResult
  {
    public string Id { get; set; }
    public string Name { get; set; }
  }

  public class PersonContact
  {
    public Guid Id { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }
    public string SubType { get; set; }
    public int Priority { get; set; }
  }
}