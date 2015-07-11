using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Website.UnitTests
{
  public static class Extensions
  {
    public static DateTime? TimeToDate(this string input)
    {
      return string.IsNullOrWhiteSpace(input) ? (DateTime?)null : DateTime.Parse("2015-01-01 " + input);
    }
  }
}
