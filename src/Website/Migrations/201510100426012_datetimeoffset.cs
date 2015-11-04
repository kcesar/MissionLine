namespace Kcesar.MissionLine.Website.Migrations
{
  using System;
  using System.Data.Entity.Migrations;

  public partial class datetimeoffset : DbMigration
  {
    public override void Up()
    {
      AlterColumn("dbo.missionline_VoiceCall", "CallTime", c => c.DateTimeOffset(nullable: false, precision: 7));
      AlterColumn("dbo.missionline_CallAction", "Time", c => c.DateTimeOffset(nullable: false, precision: 7));
      this.DeleteDefaultContraint("dbo.missionline_SarEvent", "Opened");
      AlterColumn("dbo.missionline_SarEvent", "Opened", c => c.DateTimeOffset(nullable: false, precision: 7));
      AlterColumn("dbo.missionline_SarEvent", "Closed", c => c.DateTimeOffset(precision: 7));
      AlterColumn("dbo.missionline_MemberSignIn", "TimeIn", c => c.DateTimeOffset(nullable: false, precision: 7));
      AlterColumn("dbo.missionline_MemberSignIn", "TimeOut", c => c.DateTimeOffset(precision: 7));
    }

    public override void Down()
    {
      AlterColumn("dbo.missionline_MemberSignIn", "TimeOut", c => c.DateTime());
      AlterColumn("dbo.missionline_MemberSignIn", "TimeIn", c => c.DateTime(nullable: false));
      AlterColumn("dbo.missionline_SarEvent", "Closed", c => c.DateTime());
      this.DeleteDefaultContraint("dbo.missionline_SarEvent", "Opened");
      AlterColumn("dbo.missionline_SarEvent", "Opened", c => c.DateTime(nullable: false));
      AlterColumn("dbo.missionline_CallAction", "Time", c => c.DateTime(nullable: false));
      AlterColumn("dbo.missionline_VoiceCall", "CallTime", c => c.DateTime(nullable: false));
    }
  }
}
