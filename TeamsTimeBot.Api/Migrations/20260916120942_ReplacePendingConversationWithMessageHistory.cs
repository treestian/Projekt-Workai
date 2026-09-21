using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsTimeBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePendingConversationWithMessageHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                table: "PendingConversations");

            migrationBuilder.DropColumn(
                name: "CandidateTaskIds",
                table: "PendingConversations");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "PendingConversations");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PendingConversations");

            migrationBuilder.DropColumn(
                name: "ManualMinutes",
                table: "PendingConversations");

            migrationBuilder.DropColumn(
                name: "NewTaskName",
                table: "PendingConversations");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "PendingConversations");

            migrationBuilder.DropColumn(
                name: "TaskName",
                table: "PendingConversations");

            migrationBuilder.RenameColumn(
                name: "PossibleActions",
                table: "PendingConversations",
                newName: "Messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Messages",
                table: "PendingConversations",
                newName: "PossibleActions");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "PendingConversations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CandidateTaskIds",
                table: "PendingConversations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "PendingConversations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PendingConversations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManualMinutes",
                table: "PendingConversations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewTaskName",
                table: "PendingConversations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskId",
                table: "PendingConversations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskName",
                table: "PendingConversations",
                type: "TEXT",
                nullable: true);
        }
    }
}
