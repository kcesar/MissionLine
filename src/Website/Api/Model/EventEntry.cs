using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Kcesar.MissionLine.Website.Api.Model
{
  public class EventEntry
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset Opened { get; set; }
    public DateTimeOffset? Closed { get; set; }
  }
}