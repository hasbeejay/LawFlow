using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS so this is safe to re-run against
            // databases that already received the column out-of-band.
            migrationBuilder.Sql(@"ALTER TABLE ""Messages"" ADD COLUMN IF NOT EXISTS ""Channel"" integer NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Messages"" DROP COLUMN IF EXISTS ""Channel"";");
        }
    }
}
