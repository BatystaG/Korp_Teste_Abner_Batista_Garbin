using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaturamentoService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImpressaoToken",
                table: "NotasFiscais",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImpressaoToken",
                table: "NotasFiscais");
        }
    }
}
