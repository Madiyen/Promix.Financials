using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Promix.Financials.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialPeriodsAndJournalStabilization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FinancialPeriodId",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FinancialYearId",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceDocumentId",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceDocumentNumber",
                table: "JournalEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceDocumentType",
                table: "JournalEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceLineId",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinancialPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsAdjustmentPeriod = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialPeriods_FinancialYears_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "FinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO FinancialYears (Id, CompanyId, Code, Name, StartDate, EndDate, IsActive)
                SELECT
                    NEWID(),
                    je.CompanyId,
                    CONCAT(N'MIG-', DATEPART(YEAR, je.EntryDate)),
                    CONCAT(N'سنة مالية مرحّلة ', DATEPART(YEAR, je.EntryDate)),
                    DATEFROMPARTS(DATEPART(YEAR, je.EntryDate), 1, 1),
                    DATEFROMPARTS(DATEPART(YEAR, je.EntryDate), 12, 31),
                    0
                FROM JournalEntries je
                WHERE je.IsDeleted = 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM FinancialYears fy
                      WHERE fy.CompanyId = je.CompanyId
                        AND je.EntryDate BETWEEN fy.StartDate AND fy.EndDate)
                GROUP BY je.CompanyId, DATEPART(YEAR, je.EntryDate);
                """);

            migrationBuilder.Sql("""
                DECLARE @FinancialYearId uniqueidentifier;
                DECLARE @CompanyId uniqueidentifier;
                DECLARE @StartDate date;
                DECLARE @EndDate date;
                DECLARE @PeriodStart date;
                DECLARE @PeriodEnd date;
                DECLARE @Code nvarchar(50);
                DECLARE @Name nvarchar(200);

                DECLARE period_cursor CURSOR FAST_FORWARD FOR
                SELECT fy.Id, fy.CompanyId, fy.StartDate, fy.EndDate
                FROM FinancialYears fy
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM FinancialPeriods fp
                    WHERE fp.FinancialYearId = fy.Id);

                OPEN period_cursor;
                FETCH NEXT FROM period_cursor INTO @FinancialYearId, @CompanyId, @StartDate, @EndDate;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @PeriodStart = @StartDate;

                    WHILE @PeriodStart <= @EndDate
                    BEGIN
                        SET @PeriodEnd = EOMONTH(@PeriodStart);
                        IF @PeriodEnd > @EndDate
                            SET @PeriodEnd = @EndDate;

                        SET @Code = CONCAT(
                            DATEPART(YEAR, @PeriodStart),
                            N'-',
                            RIGHT(CONCAT(N'0', DATEPART(MONTH, @PeriodStart)), 2));
                        SET @Name = CONCAT(
                            N'الفترة ',
                            DATEPART(YEAR, @PeriodStart),
                            N'/',
                            RIGHT(CONCAT(N'0', DATEPART(MONTH, @PeriodStart)), 2));

                        INSERT INTO FinancialPeriods (
                            Id,
                            CompanyId,
                            FinancialYearId,
                            Code,
                            Name,
                            StartDate,
                            EndDate,
                            Status,
                            IsAdjustmentPeriod)
                        VALUES (
                            NEWID(),
                            @CompanyId,
                            @FinancialYearId,
                            @Code,
                            @Name,
                            @PeriodStart,
                            @PeriodEnd,
                            1,
                            0);

                        SET @PeriodStart = DATEADD(DAY, 1, @PeriodEnd);
                    END

                    FETCH NEXT FROM period_cursor INTO @FinancialYearId, @CompanyId, @StartDate, @EndDate;
                END

                CLOSE period_cursor;
                DEALLOCATE period_cursor;
                """);

            migrationBuilder.Sql("""
                UPDATE je
                SET FinancialYearId = fy.Id
                FROM JournalEntries je
                CROSS APPLY (
                    SELECT TOP (1) fy.Id
                    FROM FinancialYears fy
                    WHERE fy.CompanyId = je.CompanyId
                      AND je.EntryDate BETWEEN fy.StartDate AND fy.EndDate
                    ORDER BY fy.StartDate, fy.Id
                ) fy
                WHERE je.FinancialYearId IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE je
                SET FinancialPeriodId = fp.Id
                FROM JournalEntries je
                JOIN FinancialPeriods fp
                    ON fp.CompanyId = je.CompanyId
                   AND fp.FinancialYearId = je.FinancialYearId
                   AND je.EntryDate BETWEEN fp.StartDate AND fp.EndDate
                WHERE je.FinancialPeriodId IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE JournalEntries
                SET SourceDocumentType =
                    CASE [Type]
                        WHEN 2 THEN 2
                        WHEN 3 THEN 3
                        WHEN 4 THEN 6
                        WHEN 5 THEN 5
                        WHEN 6 THEN 4
                        WHEN 7 THEN 7
                        ELSE 1
                    END
                WHERE SourceDocumentType = 0;
                """);

            migrationBuilder.Sql("""
                UPDATE JournalEntries
                SET SourceDocumentNumber = COALESCE(NULLIF(ReferenceNo, N''), EntryNumber)
                WHERE SourceDocumentNumber IS NULL;
                """);

            migrationBuilder.Sql("""
                ;WITH RankedYears AS (
                    SELECT
                        fy.Id,
                        fy.CompanyId,
                        ROW_NUMBER() OVER (
                            PARTITION BY fy.CompanyId
                            ORDER BY
                                CASE WHEN CAST(GETDATE() AS date) BETWEEN fy.StartDate AND fy.EndDate THEN 0 ELSE 1 END,
                                fy.EndDate DESC,
                                fy.StartDate DESC) AS rn
                    FROM FinancialYears fy
                )
                UPDATE fy
                SET IsActive = CASE WHEN r.rn = 1 THEN 1 ELSE fy.IsActive END
                FROM FinancialYears fy
                JOIN RankedYears r ON r.Id = fy.Id
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM FinancialYears existing
                    WHERE existing.CompanyId = fy.CompanyId
                      AND existing.IsActive = 1);
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "FinancialPeriodId",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FinancialYearId",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CompanyId_FinancialYearId_Type_EntryNumber",
                table: "JournalEntries",
                columns: new[] { "CompanyId", "FinancialYearId", "Type", "EntryNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_FinancialPeriodId",
                table: "JournalEntries",
                column: "FinancialPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_FinancialYearId",
                table: "JournalEntries",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPeriods_CompanyId_FinancialYearId_Code",
                table: "FinancialPeriods",
                columns: new[] { "CompanyId", "FinancialYearId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPeriods_CompanyId_FinancialYearId_StartDate_EndDate",
                table: "FinancialPeriods",
                columns: new[] { "CompanyId", "FinancialYearId", "StartDate", "EndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPeriods_FinancialYearId",
                table: "FinancialPeriods",
                column: "FinancialYearId");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_FinancialPeriods_FinancialPeriodId",
                table: "JournalEntries",
                column: "FinancialPeriodId",
                principalTable: "FinancialPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_FinancialYears_FinancialYearId",
                table: "JournalEntries",
                column: "FinancialYearId",
                principalTable: "FinancialYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_FinancialPeriods_FinancialPeriodId",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_FinancialYears_FinancialYearId",
                table: "JournalEntries");

            migrationBuilder.DropTable(
                name: "FinancialPeriods");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_CompanyId_FinancialYearId_Type_EntryNumber",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_FinancialPeriodId",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_FinancialYearId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "FinancialPeriodId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "FinancialYearId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "SourceDocumentId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "SourceDocumentNumber",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "SourceDocumentType",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "SourceLineId",
                table: "JournalEntries");
        }
    }
}
