/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website
{
  using System;
  using System.Data.Entity.Migrations.Infrastructure;
  using System.Data.Entity.Migrations.Model;

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

    public static DateTimeOffset ToOrgTime(this DateTimeOffset utcInput, IConfigSource config)
    {
      return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utcInput, config.GetConfig("timezone") ?? "Pacific Standard Time");
    }

    public static void DeleteDefaultContraint(this IDbMigration migration, string tableName, string colName, bool suppressTransaction = false)
    {
      var sql = new SqlOperation(String.Format(@"DECLARE @SQL varchar(1000)
        SET @SQL='ALTER TABLE {0} DROP CONSTRAINT ['+(SELECT name
        FROM sys.default_constraints
        WHERE parent_object_id = object_id('{0}')
        AND col_name(parent_object_id, parent_column_id) = '{1}')+']';
        PRINT @SQL;
        EXEC(@SQL);", tableName, colName)) { SuppressTransaction = suppressTransaction };
      migration.AddOperation(sql);
    }
  }
}
