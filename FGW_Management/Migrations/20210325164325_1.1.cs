using Microsoft.EntityFrameworkCore.Migrations;

namespace FGW_Management.Migrations
{
    public partial class _11 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_AspNetUsers_FGW_UserId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_FGW_UserId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "FGW_UserId",
                table: "Comments");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_AspNetUsers_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_AspNetUsers_UserId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_UserId",
                table: "Comments");

            migrationBuilder.AddColumn<string>(
                name: "FGW_UserId",
                table: "Comments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_FGW_UserId",
                table: "Comments",
                column: "FGW_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_AspNetUsers_FGW_UserId",
                table: "Comments",
                column: "FGW_UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
