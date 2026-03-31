using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Promix.Financials.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountPresentationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowChildren",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowManualPosting",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Classification",
                table: "Accounts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CloseBehavior",
                table: "Accounts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE Accounts
                SET AllowChildren = CASE WHEN IsPosting = 1 THEN 0 ELSE 1 END,
                    AllowManualPosting = CASE WHEN IsPosting = 1 THEN 1 ELSE 0 END,
                    Classification =
                        CASE LEFT(Code, 1)
                            WHEN '1' THEN 1
                            WHEN '2' THEN 2
                            WHEN '3' THEN 3
                            WHEN '4' THEN 4
                            WHEN '5' THEN 5
                            ELSE CASE WHEN Nature = 2 THEN 2 ELSE 1 END
                        END,
                    CloseBehavior =
                        CASE
                            WHEN LEFT(Code, 1) IN ('4', '5') THEN 3
                            ELSE 1
                        END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowChildren",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AllowManualPosting",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "CloseBehavior",
                table: "Accounts");
        }
    }
}
