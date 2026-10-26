using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudySpace.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Workspaces",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Workspaces",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Workspaces");
        }
    }
}
