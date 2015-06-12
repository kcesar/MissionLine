namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class eventDetails : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.missionline_SarEvent", "Opened", c => c.DateTime(nullable: false));
            AddColumn("dbo.missionline_SarEvent", "Closed", c => c.DateTime());
            AddColumn("dbo.missionline_SarEvent", "DirectionsText", c => c.String());
            AddColumn("dbo.missionline_SarEvent", "DirectionsUrl", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.missionline_SarEvent", "DirectionsUrl");
            DropColumn("dbo.missionline_SarEvent", "DirectionsText");
            DropColumn("dbo.missionline_SarEvent", "Closed");
            DropColumn("dbo.missionline_SarEvent", "Opened");
        }
    }
}
