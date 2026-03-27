using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Promix.Financials.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyJournalLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "JournalLockedAtUtc",
                table: "Companies",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JournalLockedByUserId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "JournalLockedThroughDate",
                table: "Companies",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JournalLockedAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "JournalLockedByUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "JournalLockedThroughDate",
                table: "Companies");
        }
    }
}
