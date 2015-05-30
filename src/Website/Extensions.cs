/*
 * Copyright 2009-2015 Matthew Cosand
 */

namespace Kcesar.MissionLine.Website
{
  public static class Extensions
  {
    public static string ToCamelCase(this string value)
    {
      if (string.IsNullOrEmpty(value))
      {
        return value;
      }
      var firstChar = value[0];
      if (char.IsLower(firstChar))
      {
        return value;
      }
      firstChar = char.ToLowerInvariant(firstChar);
      return firstChar + value.Substring(1);
    }
  }
}
