using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryAndFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Case_Country",
                table: "Cases",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_User_Country",
                table: "AspNetUsers",
                column: "Country");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Case_Country",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_User_Country",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "AspNetUsers");
        }
    }
}
