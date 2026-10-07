using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_Priority",
                table: "Tasks",
                sql: "\"Priority\" BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_TimeNeedsDate",
                table: "Tasks",
                sql: "\"DueTime\" IS NULL OR \"DueDate\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_TitleNotBlank",
                table: "Tasks",
                sql: "length(trim(\"Title\")) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_Priority",
                table: "Tasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_TimeNeedsDate",
                table: "Tasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_TitleNotBlank",
                table: "Tasks");
        }
    }
}
