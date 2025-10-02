using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvc.Migrations
{
    /// <inheritdoc />
    public partial class actualizaclien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Clientes_ClientesClienteID",
                table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Productos_ProductosProductoID",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_ClientesClienteID",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_ProductosProductoID",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ClientesClienteID",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ProductosProductoID",
                table: "Ventas");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ClienteID",
                table: "Ventas",
                column: "ClienteID");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ProductoID",
                table: "Ventas",
                column: "ProductoID");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Clientes_ClienteID",
                table: "Ventas",
                column: "ClienteID",
                principalTable: "Clientes",
                principalColumn: "ClienteID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Productos_ProductoID",
                table: "Ventas",
                column: "ProductoID",
                principalTable: "Productos",
                principalColumn: "ProductoID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Clientes_ClienteID",
                table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Productos_ProductoID",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_ClienteID",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_ProductoID",
                table: "Ventas");

            migrationBuilder.AddColumn<int>(
                name: "ClientesClienteID",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductosProductoID",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ClientesClienteID",
                table: "Ventas",
                column: "ClientesClienteID");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ProductosProductoID",
                table: "Ventas",
                column: "ProductosProductoID");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Clientes_ClientesClienteID",
                table: "Ventas",
                column: "ClientesClienteID",
                principalTable: "Clientes",
                principalColumn: "ClienteID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Productos_ProductosProductoID",
                table: "Ventas",
                column: "ProductosProductoID",
                principalTable: "Productos",
                principalColumn: "ProductoID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
