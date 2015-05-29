namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CallActions : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.missionline_CallAction",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CallId = c.Int(nullable: false),
                        Time = c.DateTime(nullable: false),
                        Action = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.missionline_VoiceCall", t => t.CallId, cascadeDelete: true)
                .Index(t => t.CallId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.missionline_CallAction", "CallId", "dbo.missionline_VoiceCall");
            DropIndex("dbo.missionline_CallAction", new[] { "CallId" });
            DropTable("dbo.missionline_CallAction");
        }
    }
}
