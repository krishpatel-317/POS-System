using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomUserRoleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_POSUsers_UserId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "POSUsers");

            migrationBuilder.DropTable(
                name: "POSRoles");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Sales",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_AspNetUsers_UserId",
                table: "Sales",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_AspNetUsers_UserId",
                table: "Sales");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Sales",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "POSRoles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSRoles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "POSUsers",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSUsers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_POSUsers_POSRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "POSRoles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_POSUsers_RoleId",
                table: "POSUsers",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_POSUsers_UserId",
                table: "Sales",
                column: "UserId",
                principalTable: "POSUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
