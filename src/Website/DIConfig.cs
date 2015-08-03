/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System;
  using System.Text.RegularExpressions;
  using Kcesar.MissionLine.Website.Data;
  using Kcesar.MissionLine.Website.Services;
  using log4net;
  using Ninject;

  public class DIConfig
  {
    public static Lazy<IKernel> CreateKernel = new Lazy<IKernel>(() =>
    {
      var kernel = new StandardKernel();
      RegisterServices(kernel);
      return kernel;
    });

    private static void RegisterServices(IKernel kernel)
    {
      var config = new ConfigSource();

      string testFile = config.GetConfig("TestMemberSource");
      if (string.IsNullOrWhiteSpace(testFile))
      {
        kernel.Bind<IMemberSource>().To<MemberSource>().InSingletonScope();
      }
      else
      {
        kernel.Bind<IMemberSource>().ToConstant(new TestMemberSource(testFile));
      }

      kernel.Bind<IConfigSource>().ToConstant(config);
      kernel.Bind<IEventsService>().To<EventsService>();
      kernel.Bind<ILog>().ToMethod(ctx => LogManager.GetLogger(ctx.Request.ParentContext.Request.ParentContext.Request.Service.FullName));

      DIConfig.databaseLogTarget = LogManager.GetLogger("database");
      Func<IMissionLineDbContext> dbFactory = () =>
      {
        var db = new MissionLineDbContext();
        db.Database.Log = FilterDatabaseLog;
        return db;
      };
      kernel.Bind<Func<IMissionLineDbContext>>().ToConstant(dbFactory);
    }

    private static ILog databaseLogTarget;
    private static Regex databaseFilterPattern = new Regex("^($|Cosed conn|Opened conn)", RegexOptions.Compiled);
    private static void FilterDatabaseLog(string message)
    {
      if (databaseLogTarget.IsDebugEnabled && !databaseFilterPattern.IsMatch(message))
      {
        databaseLogTarget.Debug(message);
      }
    }

  }
}