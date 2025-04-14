using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestingDemo.Migrations
{
    /// <inheritdoc />
    public partial class SecondAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateCompleted",
                table: "Tasks",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCompleted",
                table: "Tasks");
        }
    }
}
