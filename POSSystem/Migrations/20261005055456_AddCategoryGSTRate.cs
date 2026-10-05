using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryGSTRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GSTRate",
                table: "Categories",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GSTRate",
                table: "Categories");
        }
    }
}
