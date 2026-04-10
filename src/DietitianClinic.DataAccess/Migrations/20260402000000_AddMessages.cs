using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietitianClinic.DataAccess.Migrations
{
    public partial class AddMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messages]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[Messages](
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [SenderId] [int] NOT NULL,
                        [ReceiverId] [int] NOT NULL,
                        [Content] [nvarchar](2000) NOT NULL,
                        [IsRead] [bit] NOT NULL DEFAULT(0),
                        [AttachmentUrl] [nvarchar](500) NULL,
                        [AttachmentName] [nvarchar](255) NULL,
                        [AttachmentType] [nvarchar](20) NULL,
                        [CreatedDate] [datetime2](7) NOT NULL,
                        [ModifiedDate] [datetime2](7) NULL,
                        [DeletedDate] [datetime2](7) NULL,
                        [IsDeleted] [bit] NOT NULL DEFAULT(0),
                     CONSTRAINT [PK_Messages] PRIMARY KEY CLUSTERED 
                    (
                        [Id] ASC
                    )
                    )
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Messages_SenderId_ReceiverId' AND object_id = OBJECT_ID('Messages'))
                BEGIN
                    CREATE INDEX IX_Messages_SenderId_ReceiverId ON Messages(SenderId, ReceiverId);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Messages");
        }
    }
}
