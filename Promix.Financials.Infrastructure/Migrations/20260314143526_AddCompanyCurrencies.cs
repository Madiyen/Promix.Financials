using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Promix.Financials.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyCurrencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DecimalPlaces = table.Column<byte>(type: "tinyint", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    IsBaseCurrency = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCurrencies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCurrencies_CompanyId_CurrencyCode",
                table: "CompanyCurrencies",
                columns: new[] { "CompanyId", "CurrencyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCurrencies_CompanyId_IsBaseCurrency",
                table: "CompanyCurrencies",
                columns: new[] { "CompanyId", "IsBaseCurrency" },
                unique: true,
                filter: "[IsBaseCurrency] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyCurrencies");
        }
    }
}
