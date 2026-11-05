using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingScheduler.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailQueueAndPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiresAt",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailQueue",
                columns: table => new
                {
                    OutgoingEmailId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    To = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    MeetingId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AvailableAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailQueue", x => x.OutgoingEmailId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_SentAt_AvailableAt",
                table: "EmailQueue",
                columns: new[] { "SentAt", "AvailableAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailQueue");

            migrationBuilder.DropColumn(
                name: "PasswordResetExpiresAt",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "User");
        }
    }
}
