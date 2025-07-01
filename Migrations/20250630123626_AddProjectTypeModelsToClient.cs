using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestingDemo.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTypeModelsToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExternalAuditId",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OneTimeTransactionId",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetainershipBIRId",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetainershipSPPId",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExternalAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalAuditStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalAuditPurposes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalAuditOtherPurpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalAuditReportDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OneTimeTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeOfRegistrant = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaOfServices = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherAreaOfServices = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RetainershipBIRs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeOfRegistrant = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OCNNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOCNGenerated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateBIRRegistration = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BIRRdoNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherBirRdoNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIRCertificateUploadPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxFilingStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NeedCatchUpAccounting = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CatchUpReasons = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherCatchUpReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CatchUpStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BIRComplianceActivities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherBIRCompliance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIRRetainershipStartDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetainershipBIRs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RetainershipSPPs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SSSCompanyRegNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SSSRegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PHICCompanyRegNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PHICRegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HDMFCompanyRegNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HDMFRegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SPPComplianceActivities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherSPPCompliance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SPPRetainershipStartDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetainershipSPPs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ExternalAuditId",
                table: "Clients",
                column: "ExternalAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_OneTimeTransactionId",
                table: "Clients",
                column: "OneTimeTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_RetainershipBIRId",
                table: "Clients",
                column: "RetainershipBIRId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_RetainershipSPPId",
                table: "Clients",
                column: "RetainershipSPPId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_ExternalAudits_ExternalAuditId",
                table: "Clients",
                column: "ExternalAuditId",
                principalTable: "ExternalAudits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_OneTimeTransactions_OneTimeTransactionId",
                table: "Clients",
                column: "OneTimeTransactionId",
                principalTable: "OneTimeTransactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_RetainershipBIRs_RetainershipBIRId",
                table: "Clients",
                column: "RetainershipBIRId",
                principalTable: "RetainershipBIRs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_RetainershipSPPs_RetainershipSPPId",
                table: "Clients",
                column: "RetainershipSPPId",
                principalTable: "RetainershipSPPs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_ExternalAudits_ExternalAuditId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_OneTimeTransactions_OneTimeTransactionId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_RetainershipBIRs_RetainershipBIRId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_RetainershipSPPs_RetainershipSPPId",
                table: "Clients");

            migrationBuilder.DropTable(
                name: "ExternalAudits");

            migrationBuilder.DropTable(
                name: "OneTimeTransactions");

            migrationBuilder.DropTable(
                name: "RetainershipBIRs");

            migrationBuilder.DropTable(
                name: "RetainershipSPPs");

            migrationBuilder.DropIndex(
                name: "IX_Clients_ExternalAuditId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_OneTimeTransactionId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_RetainershipBIRId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_RetainershipSPPId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ExternalAuditId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "OneTimeTransactionId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RetainershipBIRId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RetainershipSPPId",
                table: "Clients");
        }
    }
}
