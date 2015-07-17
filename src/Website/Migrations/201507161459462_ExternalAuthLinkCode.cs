namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ExternalAuthLinkCode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.missionline_ApplicationUser", "LinkCode", c => c.String());
            AddColumn("dbo.missionline_ApplicationUser", "LinkCodeExpires", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.missionline_ApplicationUser", "LinkCodeExpires");
            DropColumn("dbo.missionline_ApplicationUser", "LinkCode");
        }
    }
}
