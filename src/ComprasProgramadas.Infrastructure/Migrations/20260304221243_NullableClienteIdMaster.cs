using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComprasProgramadas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NullableClienteIdMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContasGraficas_Clientes_ClienteId",
                table: "ContasGraficas");

            migrationBuilder.AlterColumn<long>(
                name: "ClienteId",
                table: "ContasGraficas",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_ContasGraficas_Clientes_ClienteId",
                table: "ContasGraficas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContasGraficas_Clientes_ClienteId",
                table: "ContasGraficas");

            migrationBuilder.AlterColumn<long>(
                name: "ClienteId",
                table: "ContasGraficas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContasGraficas_Clientes_ClienteId",
                table: "ContasGraficas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
