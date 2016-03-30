namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveSignins : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.missionline_UserLogin", "UserId", "dbo.missionline_ApplicationUser");
            DropIndex("dbo.missionline_UserLogin", new[] { "UserId" });
            DropTable("dbo.missionline_UserLogin");
            DropTable("dbo.missionline_ApplicationUser");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.missionline_ApplicationUser",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        UserName = c.String(),
                        LinkCode = c.String(),
                        LinkCodeExpires = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.missionline_UserLogin",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId });
            
            CreateIndex("dbo.missionline_UserLogin", "UserId");
            AddForeignKey("dbo.missionline_UserLogin", "UserId", "dbo.missionline_ApplicationUser", "Id", cascadeDelete: true);
        }
    }
}
