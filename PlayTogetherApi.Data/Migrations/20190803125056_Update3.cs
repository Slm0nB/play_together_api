using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlayTogetherApi.Data.Migrations
{
    public partial class Update3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEventSignups",
                columns: table => new
                {
                    EventId = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    SignupDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEventSignups", x => new { x.UserId, x.EventId });
                    table.ForeignKey(
                        name: "FK_UserEventSignups_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEventSignups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEventSignups_EventId",
                table: "UserEventSignups",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Games_GameId",
                table: "Events",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "GameId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Games_GameId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "UserEventSignups");
        }
    }
}
