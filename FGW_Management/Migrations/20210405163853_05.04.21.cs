using Microsoft.EntityFrameworkCore.Migrations;

namespace FGW_Management.Migrations
{
    public partial class _050421 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubmissionDue",
                table: "Submissions",
                newName: "CreationDay");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreationDay",
                table: "Submissions",
                newName: "SubmissionDue");
        }
    }
}
