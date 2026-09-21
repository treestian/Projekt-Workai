using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsTimeBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateTaskIdsToPendingConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserAzureId = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    TaskName = table.Column<string>(type: "TEXT", nullable: true),
                    TaskId = table.Column<int>(type: "INTEGER", nullable: true),
                    NewTaskName = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    ManualMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    PossibleActions = table.Column<string>(type: "TEXT", nullable: false),
                    CandidateTaskIds = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingConversations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingConversations");
        }
    }
}
