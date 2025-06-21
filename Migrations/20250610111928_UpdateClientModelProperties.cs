using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestingDemo.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClientModelProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUrgent",
                table: "Clients");

            migrationBuilder.RenameColumn(
                name: "RequirementsForPermits",
                table: "Clients",
                newName: "UrgencyLevel");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DocumentReferences",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FinancialNotes",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FinancialYear",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DocumentReferences",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "FinancialNotes",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "FinancialYear",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Clients");

            migrationBuilder.RenameColumn(
                name: "UrgencyLevel",
                table: "Clients",
                newName: "RequirementsForPermits");

            migrationBuilder.AddColumn<bool>(
                name: "IsUrgent",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
