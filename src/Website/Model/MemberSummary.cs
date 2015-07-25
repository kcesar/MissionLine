/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Model
{
  using System;
  using Newtonsoft.Json;

  public class MemberSummary
  {
    public Guid Id { get; set; }
    public string Name { get; set; }
    [JsonProperty("DEM")]
    public string WorkerNumber { get; set; }
  }
}
