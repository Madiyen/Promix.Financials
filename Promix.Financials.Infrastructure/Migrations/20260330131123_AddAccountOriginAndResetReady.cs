using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Promix.Financials.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountOriginAndResetReady : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "Accounts",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.Sql("UPDATE Accounts SET Origin = 3;");
            migrationBuilder.Sql("UPDATE Accounts SET Origin = 1 WHERE SystemRole IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Accounts");
        }
    }
}
