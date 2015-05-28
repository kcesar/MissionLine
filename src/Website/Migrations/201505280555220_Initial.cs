namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.missionline_VoiceCall",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CallId = c.String(),
                        Number = c.String(),
                        CallTime = c.DateTime(nullable: false),
                        Name = c.String(),
                        Duration = c.Int(),
                        RecordingUrl = c.String(),
                        RecordingDuration = c.Int(),
                        Comments = c.String(),
                        EventId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.missionline_SarEvent", t => t.EventId)
                .Index(t => t.EventId);
            
            CreateTable(
                "dbo.missionline_SarEvent",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        OutgoingText = c.String(),
                        OutgoingUrl = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.missionline_IssuingAuthorityKey",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.missionline_MemberSignIn",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        MemberId = c.String(),
                        TimeIn = c.DateTime(nullable: false),
                        TimeOut = c.DateTime(),
                        Miles = c.Int(),
                        EventId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.missionline_SarEvent", t => t.EventId)
                .Index(t => t.EventId);
            
            CreateTable(
                "dbo.missionline_Tenant",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.missionline_MemberSignIn", "EventId", "dbo.missionline_SarEvent");
            DropForeignKey("dbo.missionline_VoiceCall", "EventId", "dbo.missionline_SarEvent");
            DropIndex("dbo.missionline_MemberSignIn", new[] { "EventId" });
            DropIndex("dbo.missionline_VoiceCall", new[] { "EventId" });
            DropTable("dbo.missionline_Tenant");
            DropTable("dbo.missionline_MemberSignIn");
            DropTable("dbo.missionline_IssuingAuthorityKey");
            DropTable("dbo.missionline_SarEvent");
            DropTable("dbo.missionline_VoiceCall");
        }
    }
}
