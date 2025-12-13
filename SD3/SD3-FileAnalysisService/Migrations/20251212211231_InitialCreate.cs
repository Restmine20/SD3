using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SD3_FileAnalisysService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AssignmentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsPlagiarismDetected = table.Column<bool>(type: "boolean", nullable: false),
                    PlagiarismPercentage = table.Column<int>(type: "integer", nullable: false),
                    ReportContent = table.Column<string>(type: "text", nullable: false),
                    AnalysisTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WordCloudPath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.ReportId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reports");
        }
    }
}
