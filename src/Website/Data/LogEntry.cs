using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kcesar.MissionLine.Website.Data
{
  public class LogEntry
  {
    [Column("id")]
    public long Id { get; set; }

    [Column("time")]
    public DateTime Time { get; set; }

    [Column("user")]
    public string User { get; set; }

    [Column("level")]
    public string Level { get; set; }

    [Column("source")]
    public string Source { get; set; }

    [Column("message")]
    public string Message { get; set; }

    [Column("details")]
    public string Details { get; set; }
  }
}