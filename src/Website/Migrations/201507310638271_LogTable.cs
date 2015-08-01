namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LogTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.missionline_LogEntry",
                c => new
                    {
                        id = c.Long(nullable: false, identity: true),
                        time = c.DateTime(nullable: false),
                        user = c.String(),
                        level = c.String(),
                        source = c.String(),
                        message = c.String(),
                        details = c.String(),
                    })
                .PrimaryKey(t => t.id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.missionline_LogEntry");
        }
    }
}
