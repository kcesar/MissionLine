using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Kcesar.MissionLine.Website.Data
{
  public class MemberSignIn
  {
    public int Id { get; set; }

    public string Name { get; set; }
    public string MemberId { get; set; }
    public bool isMember { get; set; }

    public DateTime TimeIn { get; set; }
    public DateTime? TimeOut { get; set; }
    public int? Miles { get; set; }

    [ForeignKey("Event")]
    public int? EventId { get; set; }
    public virtual SarEvent Event { get; set; }

    public override string ToString()
    {
      return string.Format("{0} @{1:yyyy-MM-dd HH:mm} - {2:yyyy-MM-dd HH:mm}", this.Name, this.TimeIn, this.TimeOut);
    }
  }
}