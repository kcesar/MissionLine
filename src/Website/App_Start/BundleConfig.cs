/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System.Web;
  using System.Web.Optimization;

  public class BundleConfig
  {
    // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
    public static void RegisterBundles(BundleCollection bundles)
    {
      bundles.Add(new ScriptBundle("~/bundles/core").Include(
                  "~/Scripts/jquery-{version}.js",
                   "~/Scripts/bootstrap.js",
                   "~/Scripts/bootstrap-select.js",
                   "~/Scripts/bootstrap-dialog.js",
                  "~/Scripts/respond.js",
                  "~/Scripts/moment.js",
                  "~/Scripts/angular.js",
                  "~/Scripts/angular-modal-service.js",
            //      "~/Scripts/knockout-{version}.js",
            //      "~/Scripts/site/ko-bindings.js",
                  "~/Scripts/jquery.signalR-{version}.js",
                  "~/Scripts/jquery.toaster.js"
           //       "~/Scripts/site/forms.js"
           ));

      bundles.Add(new ScriptBundle("~/bundles/page/index").Include(
              //    "~/Scripts/site/page/index.js"));
              "~/app/js/controllers.js"));

      bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                  "~/Scripts/jquery.validate*"));

      // Use the development version of Modernizr to develop with and learn from. Then, when you're
      // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
      bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                  "~/Scripts/modernizr-*"));

      bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/bootstrap.css",
                "~/Content/bootstrap-select.css",
                "~/Content/bootstrap-dialog.css",
                "~/Content/font-awesome.css",
                "~/Content/site.css"));
    }
  }
}
