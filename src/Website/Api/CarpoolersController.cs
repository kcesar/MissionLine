﻿using Kcesar.MissionLine.Website.Api.Model;
using Kcesar.MissionLine.Website.Data;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Kcesar.MissionLine.Website.Api
{
  public class CarpoolersController : ApiController
  {
    private readonly IConfigSource config;
    private readonly Func<IMissionLineDbContext> dbFactory;
    private readonly IMemberSource memberSource;

    public CarpoolersController(Func<IMissionLineDbContext> dbFactory, IConfigSource config, IMemberSource memberSource)
    {
      this.dbFactory = dbFactory;
      this.config = config;
      this.memberSource = memberSource;
    }

    /// <summary>
    /// Gets the carpoolers for the given event ID
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    // GET: api/carpooling/5
    [Route("api/events/{eventId}/carpoolers")]
    public async Task<List<CarpoolerEntry>> Get(int eventId)
    {
      // Get the carpoolers
      Carpooler[] carpoolers;
      using (var db = dbFactory())
      {
        carpoolers = await db.Carpoolers.Where(i => i.EventId == eventId).ToArrayAsync();
      }

      // Look up their member info
      var members = await memberSource.TryLookupMembersAsync(carpoolers.Select(i => i.MemberId));

      // And join the two
      List<CarpoolerEntry> answer = new List<CarpoolerEntry>();
      foreach (var carpooler in carpoolers)
      {
        var member = members.FirstOrDefault(i => i.Id == carpooler.MemberId);
        if (member != null)
        {
          answer.Add(new CarpoolerEntry(carpooler)
          {
            Member = member
          });
        }
      }
      return answer;
    }

    // Contact types to return (and the order to return them in)
    private readonly static string[] s_filterContactTypes = new string[]
    {
      "phone",
      "email"
    };

    [Route("api/events/{eventId}/carpoolers/{memberId}")]
    public async Task<CarpoolerEntry> Get(int eventId, string memberId)
    {
      // Get the carpooler
      Carpooler carpooler;
      using (var db = dbFactory())
      {
        carpooler = await db.Carpoolers.FirstOrDefaultAsync(i => i.EventId == eventId && i.MemberId == memberId);
      }

      if (carpooler == null)
      {
        return null;
      }

      // Look up their member info
      var member = (await memberSource.TryLookupMembersAsync(new string[] { memberId })).FirstOrDefault();
      if (member == null)
      {
        return null;
      }

      // Join the two
      return new CarpoolerEntry(carpooler)
      {
        Member = member,
        PersonContacts = await GetPersonContactsAsync(memberId)
      };
    }

    [Route("api/events/{eventId}/carpoolers/{memberId}/updateinfo")]
    public async Task<CarpoolerEntry> GetUpdateInfo(int eventId, string memberId)
    {
      // Get the carpooler
      Carpooler carpooler;
      using (var db = dbFactory())
      {
        carpooler = await db.Carpoolers.FirstOrDefaultAsync(i => i.EventId == eventId && i.MemberId == memberId);

        // If we didn't find one, load info from a previous event
        if (carpooler == null)
        {
          carpooler = await db.Carpoolers.Where(i => i.MemberId == memberId).OrderByDescending(i => i.EventId).FirstOrDefaultAsync();
        }
      }

      // No need to load the member info

      // If we didn't find a carpooler entry, use the defaults
      if (carpooler == null)
      {
        carpooler = new Carpooler();
      }

      // Join with their contact info
      return new CarpoolerEntry(carpooler)
      {
        PersonContacts = await GetPersonContactsAsync(memberId)
      };
    }

    /// <summary>
    /// API to obtain the carpooler's last known carpooler location.
    /// </summary>
    /// <param name="memberId"></param>
    /// <returns></returns>
    [Route("api/carpoolers/{memberId}/previouslocation")]
    public async Task<Location> GetPreviousLocation(string memberId)
    {
      // Get the carpooler
      Carpooler carpooler;
      using (var db = dbFactory())
      {
        carpooler = await db.Carpoolers.Where(i => i.MemberId == memberId).OrderByDescending(i => i.EventId).FirstOrDefaultAsync();
      }

      // No need to load the member info

      // If we didn't find a carpooler entry, return null
      if (carpooler == null)
      {
        return null;
      }

      if (carpooler.LocationLatitude != 0 && carpooler.LocationLongitude != 0)
      {
        return new Location()
        {
          Latitude = carpooler.LocationLatitude,
          Longitude = carpooler.LocationLongitude
        };
      }

      return null;
    }

    private async Task<List<PersonContact>> GetPersonContactsAsync(string memberId)
    {
      return (await memberSource.LookupPersonContactsAsync(memberId))
          .Where(i => s_filterContactTypes.Contains(i.Type))
          .OrderBy(i => i.Type, new PersonContactTypeComparer())
          .ToList();
    }

    private class PersonContactTypeComparer : IComparer<string>
    {
      public int Compare(string x, string y)
      {
        int xIndex = Array.IndexOf(s_filterContactTypes, x);
        int yIndex = Array.IndexOf(s_filterContactTypes, y);

        return xIndex.CompareTo(yIndex);
      }
    }

    // POST
    [HttpPost]
    [Route("api/events/{eventId}/carpoolers/{memberId}")]
    public async Task Post(int eventId, string memberId, [FromBody]CarpoolerEntry updatedInfo)
    {
      using (var db = dbFactory())
      {
        var carpooler = await db.Carpoolers.FirstOrDefaultAsync(i => i.EventId == eventId && i.MemberId == memberId);

        bool added = false;
        if (carpooler == null)
        {
          carpooler = new Carpooler()
          {
            EventId = eventId,
            MemberId = memberId
          };

          added = true;
        }

        if (updatedInfo.CanBeDriver != null)
        {
          carpooler.CanBeDriver = updatedInfo.CanBeDriver.Value;
        }

        if (updatedInfo.CanBePassenger != null)
        {
          carpooler.CanBePassenger = updatedInfo.CanBePassenger.Value;
        }

        if (updatedInfo.LocationLatitude != null)
        {
          carpooler.LocationLatitude = updatedInfo.LocationLatitude.Value;
        }

        if (updatedInfo.LocationLongitude != null)
        {
          carpooler.LocationLongitude = updatedInfo.LocationLongitude.Value;
        }

        if (updatedInfo.VehicleDescription != null)
        {
          carpooler.VehicleDescription = updatedInfo.VehicleDescription;
        }

        if (updatedInfo.Message != null)
        {
          carpooler.Message = updatedInfo.Message;
        }

        if (!carpooler.CanBeDriver && !carpooler.CanBePassenger)
        {
          throw new InvalidOperationException("Must at least be either a passenger or driver. If you wish to remove, use the DELETE command");
        }

        if (added)
        {
          db.Carpoolers.Add(carpooler);
        }
        await db.SaveChangesAsync();
        NotifyCarpoolersChanged(eventId);
      }
    }

    // DELETE
    [Route("api/events/{eventId}/carpoolers/{memberId}")]
    public async Task Delete(int eventId, string memberId)
    {
      using (var db = dbFactory())
      {
        var carpooler = await db.Carpoolers.FirstOrDefaultAsync(i => i.EventId == eventId && i.MemberId == memberId);
        if (carpooler != null)
        {
          db.Carpoolers.Remove(carpooler);

          await db.SaveChangesAsync();
          NotifyCarpoolersChanged(eventId);
        }
      }
    }

    private void NotifyCarpoolersChanged(int eventId)
    {
      this.config.GetPushHub<CallsHub>().carpoolersChanged(eventId);
    }
  }
}
