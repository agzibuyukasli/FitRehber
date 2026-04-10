using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietitianClinic.DataAccess.Migrations
{
    public partial class AddDailyTrackings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DailyTrackings]') AND type = N'U')
                BEGIN
                    CREATE TABLE [dbo].[DailyTrackings](
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [PatientId] [int] NOT NULL,
                        [TrackingDate] [date] NOT NULL,
                        [WaterLiters] [float] NULL,
                        [StepsCount] [int] NULL,
                        [CreatedDate] [datetime2](7) NOT NULL,
                        [ModifiedDate] [datetime2](7) NULL,
                        [DeletedDate] [datetime2](7) NULL,
                        [IsDeleted] [bit] NOT NULL DEFAULT(0),
                        CONSTRAINT [PK_DailyTrackings] PRIMARY KEY CLUSTERED ([Id] ASC),
                        CONSTRAINT [UQ_DailyTrackings_Patient_Date] UNIQUE ([PatientId], [TrackingDate])
                    )
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DailyTrackings_PatientId_TrackingDate' AND object_id = OBJECT_ID('DailyTrackings'))
                BEGIN
                    CREATE INDEX IX_DailyTrackings_PatientId_TrackingDate ON DailyTrackings(PatientId, TrackingDate);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DailyTrackings");
        }
    }
}
