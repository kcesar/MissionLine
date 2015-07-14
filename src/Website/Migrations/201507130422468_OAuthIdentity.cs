namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OAuthIdentity : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.missionline_Role",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
            CreateTable(
                "dbo.missionline_UserRole",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                        IdentityUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.missionline_Role", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.missionline_User", t => t.IdentityUser_Id)
                .Index(t => t.RoleId)
                .Index(t => t.IdentityUser_Id);
            
            CreateTable(
                "dbo.missionline_User",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.missionline_UserClaim",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                        IdentityUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.missionline_User", t => t.IdentityUser_Id)
                .Index(t => t.IdentityUser_Id);
            
            CreateTable(
                "dbo.missionline_UserLogin",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                        IdentityUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.missionline_User", t => t.IdentityUser_Id)
                .Index(t => t.IdentityUser_Id);
            
            CreateTable(
                "dbo.missionline_AppUser",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.missionline_User", t => t.Id)
                .Index(t => t.Id);
            
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
            
            DropForeignKey("dbo.missionline_AppUser", "Id", "dbo.missionline_User");
            DropForeignKey("dbo.missionline_UserRole", "IdentityUser_Id", "dbo.missionline_User");
            DropForeignKey("dbo.missionline_UserLogin", "IdentityUser_Id", "dbo.missionline_User");
            DropForeignKey("dbo.missionline_UserClaim", "IdentityUser_Id", "dbo.missionline_User");
            DropForeignKey("dbo.missionline_UserRole", "RoleId", "dbo.missionline_Role");
            DropIndex("dbo.missionline_AppUser", new[] { "Id" });
            DropIndex("dbo.missionline_UserLogin", new[] { "IdentityUser_Id" });
            DropIndex("dbo.missionline_UserClaim", new[] { "IdentityUser_Id" });
            DropIndex("dbo.missionline_User", "UserNameIndex");
            DropIndex("dbo.missionline_UserRole", new[] { "IdentityUser_Id" });
            DropIndex("dbo.missionline_UserRole", new[] { "RoleId" });
            DropIndex("dbo.missionline_Role", "RoleNameIndex");
            DropTable("dbo.missionline_AppUser");
            DropTable("dbo.missionline_UserLogin");
            DropTable("dbo.missionline_UserClaim");
            DropTable("dbo.missionline_User");
            DropTable("dbo.missionline_UserRole");
            DropTable("dbo.missionline_Role");
        }
    }
}
