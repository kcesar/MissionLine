using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Kcesar.MissionLine.Website.Data
{
  public class SarEvent
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string OutgoingText { get; set; }
    public string OutgoingUrl { get; set; }
  }
}