using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietitianClinic.DataAccess.Migrations
{
    public partial class AddUserLockout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'AccessFailedCount')
                    ALTER TABLE Users ADD AccessFailedCount int NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'LockoutEndUtc')
                    ALTER TABLE Users ADD LockoutEndUtc datetime2 NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE Users DROP COLUMN IF EXISTS AccessFailedCount;
                ALTER TABLE Users DROP COLUMN IF EXISTS LockoutEndUtc;
            ");
        }
    }
}
