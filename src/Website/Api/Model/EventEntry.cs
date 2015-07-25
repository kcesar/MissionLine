/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Api.Model
{
  using System;

  public class EventEntry
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset Opened { get; set; }
    public DateTimeOffset? Closed { get; set; }
  }
}