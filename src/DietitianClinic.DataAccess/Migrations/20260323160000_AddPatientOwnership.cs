using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietitianClinic.DataAccess.Migrations
{
    public partial class AddPatientOwnership : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Patients' AND COLUMN_NAME = 'UserId')
                BEGIN
                    ALTER TABLE Patients ADD UserId int NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Patients_UserId' AND object_id = OBJECT_ID('Patients'))
                BEGIN
                    CREATE INDEX IX_Patients_UserId ON Patients(UserId);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Patients_Users_UserId')
                BEGIN
                    ALTER TABLE Patients ADD CONSTRAINT FK_Patients_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Users_UserId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_UserId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Patients");
        }
    }
}
