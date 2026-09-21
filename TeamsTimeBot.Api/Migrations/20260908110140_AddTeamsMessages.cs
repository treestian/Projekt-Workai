using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsTimeBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamsMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GraphMessageId = table.Column<string>(type: "TEXT", nullable: false),
                    TeamId = table.Column<string>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<string>(type: "TEXT", nullable: false),
                    SenderAzureId = table.Column<string>(type: "TEXT", nullable: true),
                    SenderName = table.Column<string>(type: "TEXT", nullable: true),
                    MessageText = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamsMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamsMessages_GraphMessageId",
                table: "TeamsMessages",
                column: "GraphMessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamsMessages");
        }
    }
}
