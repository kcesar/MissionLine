namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class linkToMember : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.missionline_MemberSignIn", "isMember", c => c.Boolean(nullable: false, defaultValue: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.missionline_MemberSignIn", "isMember");
        }
    }
}
