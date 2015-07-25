/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Data
{
  using System;
  using System.ComponentModel.DataAnnotations.Schema;

  public class CallAction
  {
    public int Id { get; set; }

    [ForeignKey("Call")]
    public int CallId { get; set; }
    public virtual VoiceCall Call { get; set; }

    public DateTime Time { get; set; }
    public string Action { get; set; }
  }
}