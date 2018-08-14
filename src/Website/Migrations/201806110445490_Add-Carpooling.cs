namespace Kcesar.MissionLine.Website.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCarpooling : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "missionline.Carpoolers",
                c => new
                    {
                        EventId = c.Int(nullable: false),
                        MemberId = c.String(nullable: false, maxLength: 128),
                        CanBeDriver = c.Boolean(nullable: false),
                        CanBePassenger = c.Boolean(nullable: false),
                        LocationLatitude = c.Decimal(nullable: false, precision: 18, scale: 12),
                        LocationLongitude = c.Decimal(nullable: false, precision: 18, scale: 12),
                        VehicleDescription = c.String(),
                        Message = c.String(),
                    })
                .PrimaryKey(t => new { t.EventId, t.MemberId });
            
        }
        
        public override void Down()
        {
            DropTable("missionline.Carpoolers");
        }
    }
}
