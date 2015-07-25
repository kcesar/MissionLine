/*
 * Copyright 2015 Matthew Cosand
 */
namespace Website.UnitTests
{
  using System;

  public static class Extensions
  {
    public static DateTime? TimeToDate(this string input)
    {
      return string.IsNullOrWhiteSpace(input) ? (DateTime?)null : DateTime.Parse("2015-01-01 " + input);
    }
  }
}
