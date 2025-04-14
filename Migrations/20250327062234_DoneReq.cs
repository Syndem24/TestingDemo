using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestingDemo.Migrations
{
    /// <inheritdoc />
    public partial class DoneReq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DoneRequirements",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoneRequirements",
                table: "Tasks");
        }
    }
}
