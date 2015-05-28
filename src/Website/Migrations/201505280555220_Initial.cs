namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.VoiceCalls",
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
                .ForeignKey("dbo.SarEvents", t => t.EventId)
                .Index(t => t.EventId);
            
            CreateTable(
                "dbo.SarEvents",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        OutgoingText = c.String(),
                        OutgoingUrl = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.IssuingAuthorityKeys",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.MemberSignIns",
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
                .ForeignKey("dbo.SarEvents", t => t.EventId)
                .Index(t => t.EventId);
            
            CreateTable(
                "dbo.Tenants",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MemberSignIns", "EventId", "dbo.SarEvents");
            DropForeignKey("dbo.VoiceCalls", "EventId", "dbo.SarEvents");
            DropIndex("dbo.MemberSignIns", new[] { "EventId" });
            DropIndex("dbo.VoiceCalls", new[] { "EventId" });
            DropTable("dbo.Tenants");
            DropTable("dbo.MemberSignIns");
            DropTable("dbo.IssuingAuthorityKeys");
            DropTable("dbo.SarEvents");
            DropTable("dbo.VoiceCalls");
        }
    }
}
