using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Promix.Financials.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalCurrencyAndTreasuryNormalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrencyAmount",
                table: "JournalEntries",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "JournalEntries",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "JournalEntries",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE je
                SET
                    CurrencyCode = COALESCE(baseCurrency.CurrencyCode, N'SYP'),
                    ExchangeRate = 1,
                    CurrencyAmount = totals.TotalDebit
                FROM JournalEntries je
                OUTER APPLY
                (
                    SELECT TOP (1) cc.CurrencyCode
                    FROM CompanyCurrencies cc
                    WHERE cc.CompanyId = je.CompanyId
                      AND cc.IsBaseCurrency = 1
                      AND cc.IsActive = 1
                ) baseCurrency
                OUTER APPLY
                (
                    SELECT CAST(ISNULL(SUM(jl.Debit), 0) AS decimal(18,4)) AS TotalDebit
                    FROM JournalLines jl
                    WHERE jl.JournalEntryId = je.Id
                ) totals;
                """);

            migrationBuilder.Sql(
                """
                UPDATE a
                SET IsPosting = 1
                FROM Accounts a
                WHERE a.Code IN (N'131', N'132')
                  AND a.IsPosting = 0
                  AND a.IsActive = 1
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM Accounts child
                      WHERE child.CompanyId = a.CompanyId
                        AND child.ParentId = a.Id
                  );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO Accounts
                (
                    Id,
                    CompanyId,
                    Code,
                    NameAr,
                    NameEn,
                    Nature,
                    IsPosting,
                    ParentId,
                    CurrencyCode,
                    SystemRole,
                    Notes,
                    IsSystem,
                    IsActive
                )
                SELECT
                    NEWID(),
                    parent.CompanyId,
                    N'133',
                    N'الخزنة الرئيسية',
                    NULL,
                    1,
                    1,
                    parent.Id,
                    NULL,
                    NULL,
                    N'__AUTO_MAIN_TREASURY__',
                    0,
                    1
                FROM Accounts parent
                WHERE parent.Code = N'13'
                  AND parent.CompanyId IS NOT NULL
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM Accounts existing
                      WHERE existing.CompanyId = parent.CompanyId
                        AND existing.Code = N'133'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM Accounts
                WHERE Code = N'133'
                  AND Notes = N'__AUTO_MAIN_TREASURY__'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM JournalLines jl
                      WHERE jl.AccountId = Accounts.Id
                  );
                """);

            migrationBuilder.DropColumn(
                name: "CurrencyAmount",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "JournalEntries");
        }
    }
}
