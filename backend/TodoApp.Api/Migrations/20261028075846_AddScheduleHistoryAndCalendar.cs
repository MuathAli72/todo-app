using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleHistoryAndCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarFeeds",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarFeeds", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_CalendarFeeds_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoutineSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    RoutineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Days = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineSchedules_Routines_RoutineId",
                        column: x => x.RoutineId,
                        principalTable: "Routines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarFeeds_Token",
                table: "CalendarFeeds",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutineSchedules_RoutineId",
                table: "RoutineSchedules",
                column: "RoutineId");

            migrationBuilder.Sql("INSERT INTO \"RoutineSchedules\" (\"Id\", \"RoutineId\", \"StartsOn\", \"Days\") SELECT gen_random_uuid(), \"Id\", \"ScheduleStartsOn\", \"Days\" FROM \"Routines\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarFeeds");

            migrationBuilder.DropTable(
                name: "RoutineSchedules");
        }

    }
}
