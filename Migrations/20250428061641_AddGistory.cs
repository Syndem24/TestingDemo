using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestingDemo.Migrations
{
    /// <inheritdoc />
    public partial class AddGistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMovedToHistory",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMovedToHistory",
                table: "Tasks");
        }
    }
}
