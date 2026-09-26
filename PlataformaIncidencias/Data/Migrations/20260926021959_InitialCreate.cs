using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PlataformaIncidencias.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Estacion = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    Prioridad = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidencias", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Incidencias",
                columns: new[] { "Id", "Descripcion", "Estacion", "Estado", "FechaCreacion", "Prioridad" },
                values: new object[,]
                {
                    { 1, "Bicicleta con freno delantero roto", "Estación Central", "Abierta", new DateTime(2025, 1, 10, 8, 0, 0, 0, DateTimeKind.Utc), "Alta" },
                    { 2, "Candado bloqueado, no libera bicicleta", "Estación Norte", "Abierta", new DateTime(2025, 1, 10, 9, 0, 0, 0, DateTimeKind.Utc), "Alta" },
                    { 3, "Panel solar del dock sin carga", "Estación Sur", "Abierta", new DateTime(2025, 1, 11, 10, 0, 0, 0, DateTimeKind.Utc), "Media" },
                    { 4, "Rueda trasera pinchada en bicicleta #42", "Estación Este", "Abierta", new DateTime(2025, 1, 11, 11, 0, 0, 0, DateTimeKind.Utc), "Alta" },
                    { 5, "Pantalla del kiosco no responde al tacto", "Estación Oeste", "Abierta", new DateTime(2025, 1, 12, 8, 30, 0, 0, DateTimeKind.Utc), "Media" },
                    { 6, "Sillín roto en bicicleta #17", "Estación Central", "Cerrada", new DateTime(2025, 1, 9, 15, 0, 0, 0, DateTimeKind.Utc), "Baja" },
                    { 7, "Luz delantera no funciona en bicicleta #5", "Estación Norte", "Cerrada", new DateTime(2025, 1, 9, 16, 0, 0, 0, DateTimeKind.Utc), "Baja" },
                    { 8, "Conector de carga dañado en dock #3", "Estación Sur", "Abierta", new DateTime(2025, 1, 13, 9, 0, 0, 0, DateTimeKind.Utc), "Media" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incidencias");
        }
    }
}
