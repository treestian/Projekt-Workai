using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsTimeBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkLogStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WorkLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "WorkLogs");
        }
    }
}
