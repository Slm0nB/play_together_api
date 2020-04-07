using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlayTogetherApi.Data.Migrations
{
    public partial class Update12 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "UtcOffset",
                table: "Users",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UtcOffset",
                table: "Users");
        }
    }
}
