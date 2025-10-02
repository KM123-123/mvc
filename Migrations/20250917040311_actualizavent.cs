using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvc.Migrations
{
    /// <inheritdoc />
    public partial class actualizavent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalVenta",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Ventas");

            migrationBuilder.AddColumn<int>(
                name: "Cantidad",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClienteID",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClientesClienteID",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductoID",
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

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Ventas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioID",
                table: "Ventas",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ClientesClienteID",
                table: "Ventas",
                column: "ClientesClienteID");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ProductosProductoID",
                table: "Ventas",
                column: "ProductosProductoID");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_UsuarioID",
                table: "Ventas",
                column: "UsuarioID");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioID",
                table: "Ventas",
                column: "UsuarioID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioID",
                table: "Ventas");

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

            migrationBuilder.DropIndex(
                name: "IX_Ventas_UsuarioID",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ClienteID",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ClientesClienteID",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ProductoID",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ProductosProductoID",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioID",
                table: "Ventas");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalVenta",
                table: "Ventas",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Ventas",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }
    }
}
