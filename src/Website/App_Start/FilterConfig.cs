/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System.Web;
  using System.Web.Mvc;

  public class FilterConfig
  {
    public static void RegisterGlobalFilters(GlobalFilterCollection filters)
    {
      filters.Add(new AuthorizeAttribute());
      filters.Add(new HandleErrorAttribute());
    }
  }
}
