using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvc.Migrations
{
    /// <inheritdoc />
    public partial class ventas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Categoria_CategoriaID",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Proveedores_ProveedorID",
                table: "Productos");

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    VentaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaVenta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TotalVenta = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.VentaID);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Categoria_CategoriaID",
                table: "Productos",
                column: "CategoriaID",
                principalTable: "Categoria",
                principalColumn: "CategoriaID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Proveedores_ProveedorID",
                table: "Productos",
                column: "ProveedorID",
                principalTable: "Proveedores",
                principalColumn: "ProveedorID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Categoria_CategoriaID",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Proveedores_ProveedorID",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Categoria_CategoriaID",
                table: "Productos",
                column: "CategoriaID",
                principalTable: "Categoria",
                principalColumn: "CategoriaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Proveedores_ProveedorID",
                table: "Productos",
                column: "ProveedorID",
                principalTable: "Proveedores",
                principalColumn: "ProveedorID");
        }
    }
}
