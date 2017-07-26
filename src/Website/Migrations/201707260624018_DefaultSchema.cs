namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DefaultSchema : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.missionline_VoiceCall", newName: "VoiceCalls");
            RenameTable(name: "dbo.missionline_CallAction", newName: "CallActions");
            RenameTable(name: "dbo.missionline_SarEvent", newName: "SarEvents");
            RenameTable(name: "dbo.missionline_MemberSignIn", newName: "MemberSignIns");
            RenameTable(name: "dbo.missionline_LogEntry", newName: "LogEntries");
            MoveTable(name: "dbo.VoiceCalls", newSchema: "missionline");
            MoveTable(name: "dbo.CallActions", newSchema: "missionline");
            MoveTable(name: "dbo.SarEvents", newSchema: "missionline");
            MoveTable(name: "dbo.MemberSignIns", newSchema: "missionline");
            MoveTable(name: "dbo.LogEntries", newSchema: "missionline");
        }
        
        public override void Down()
        {
            MoveTable(name: "missionline.LogEntries", newSchema: "dbo");
            MoveTable(name: "missionline.MemberSignIns", newSchema: "dbo");
            MoveTable(name: "missionline.SarEvents", newSchema: "dbo");
            MoveTable(name: "missionline.CallActions", newSchema: "dbo");
            MoveTable(name: "missionline.VoiceCalls", newSchema: "dbo");
            RenameTable(name: "dbo.LogEntries", newName: "missionline_LogEntry");
            RenameTable(name: "dbo.MemberSignIns", newName: "missionline_MemberSignIn");
            RenameTable(name: "dbo.SarEvents", newName: "missionline_SarEvent");
            RenameTable(name: "dbo.CallActions", newName: "missionline_CallAction");
            RenameTable(name: "dbo.VoiceCalls", newName: "missionline_VoiceCall");
        }
    }
}
