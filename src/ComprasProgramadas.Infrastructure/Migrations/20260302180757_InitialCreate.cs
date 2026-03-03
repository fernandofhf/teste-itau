using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComprasProgramadas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CestasRecomendacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataDesativacao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CestasRecomendacao", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CPF = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorMensal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DataAdesao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataSaida = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cotacoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DataPregao = table.Column<DateOnly>(type: "date", nullable: false),
                    Ticker = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrecoAbertura = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecoFechamento = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecoMaximo = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecoMinimo = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotacoes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrdensCompra",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ContaMasterId = table.Column<long>(type: "bigint", nullable: false),
                    Ticker = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TipoMercado = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataExecucao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdensCompra", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItensCesta",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CestaId = table.Column<long>(type: "bigint", nullable: false),
                    Ticker = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Percentual = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensCesta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensCesta_CestasRecomendacao_CestaId",
                        column: x => x.CestaId,
                        principalTable: "CestasRecomendacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ContasGraficas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    NumeroConta = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasGraficas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasGraficas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EventosIR",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorIR = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PublicadoKafka = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataEvento = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosIR", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosIR_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Rebalanceamentos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TickerVendido = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TickerComprado = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorVenda = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataRebalanceamento = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rebalanceamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rebalanceamentos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Custodias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ContaGraficaId = table.Column<long>(type: "bigint", nullable: false),
                    Ticker = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    PrecoMedio = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DataUltimaAtualizacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Custodias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Custodias_ContasGraficas_ContaGraficaId",
                        column: x => x.ContaGraficaId,
                        principalTable: "ContasGraficas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Distribuicoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrdemCompraId = table.Column<long>(type: "bigint", nullable: false),
                    CustodiaFilhoteId = table.Column<long>(type: "bigint", nullable: false),
                    Ticker = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DataDistribuicao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distribuicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Distribuicoes_Custodias_CustodiaFilhoteId",
                        column: x => x.CustodiaFilhoteId,
                        principalTable: "Custodias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Distribuicoes_OrdensCompra_OrdemCompraId",
                        column: x => x.OrdemCompraId,
                        principalTable: "OrdensCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_CPF",
                table: "Clientes",
                column: "CPF",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContasGraficas_ClienteId",
                table: "ContasGraficas",
                column: "ClienteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContasGraficas_NumeroConta",
                table: "ContasGraficas",
                column: "NumeroConta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cotacoes_Ticker_DataPregao",
                table: "Cotacoes",
                columns: new[] { "Ticker", "DataPregao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Custodias_ContaGraficaId_Ticker",
                table: "Custodias",
                columns: new[] { "ContaGraficaId", "Ticker" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Distribuicoes_CustodiaFilhoteId",
                table: "Distribuicoes",
                column: "CustodiaFilhoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Distribuicoes_OrdemCompraId",
                table: "Distribuicoes",
                column: "OrdemCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosIR_ClienteId",
                table: "EventosIR",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensCesta_CestaId",
                table: "ItensCesta",
                column: "CestaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rebalanceamentos_ClienteId",
                table: "Rebalanceamentos",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cotacoes");

            migrationBuilder.DropTable(
                name: "Distribuicoes");

            migrationBuilder.DropTable(
                name: "EventosIR");

            migrationBuilder.DropTable(
                name: "ItensCesta");

            migrationBuilder.DropTable(
                name: "Rebalanceamentos");

            migrationBuilder.DropTable(
                name: "Custodias");

            migrationBuilder.DropTable(
                name: "OrdensCompra");

            migrationBuilder.DropTable(
                name: "CestasRecomendacao");

            migrationBuilder.DropTable(
                name: "ContasGraficas");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
