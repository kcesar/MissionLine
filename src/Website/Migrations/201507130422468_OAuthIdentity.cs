namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OAuthIdentity : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.missionline_UserLogin",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.missionline_ApplicationUser", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.missionline_ApplicationUser",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        UserName = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            DropTable("dbo.missionline_IssuingAuthorityKey");
            DropTable("dbo.missionline_Tenant");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.missionline_Tenant",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.missionline_IssuingAuthorityKey",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
            DropForeignKey("dbo.missionline_UserLogin", "UserId", "dbo.missionline_ApplicationUser");
            DropIndex("dbo.missionline_UserLogin", new[] { "UserId" });
            DropTable("dbo.missionline_ApplicationUser");
            DropTable("dbo.missionline_UserLogin");
        }
    }
}
