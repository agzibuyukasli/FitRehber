using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietitianClinic.DataAccess.Migrations
{
    public partial class AddEmailNotificationsEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Patients') AND name = 'EmailNotificationsEnabled')
                    ALTER TABLE Patients ADD EmailNotificationsEnabled bit NOT NULL DEFAULT 1;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Patients') AND name = 'EmailNotificationsEnabled')
                    ALTER TABLE Patients DROP COLUMN EmailNotificationsEnabled;
            ");
        }
    }
}
