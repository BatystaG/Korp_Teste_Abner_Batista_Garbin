using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaturamentoService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStatusValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Atualiza status existentes: Rascunho -> Aberta, Impressa -> Fechada
            migrationBuilder.Sql("UPDATE \"NotasFiscais\" SET \"Status\" = 'Aberta' WHERE \"Status\" = 'Rascunho'");
            migrationBuilder.Sql("UPDATE \"NotasFiscais\" SET \"Status\" = 'Fechada' WHERE \"Status\" = 'Impressa'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverte as mudanças: Aberta -> Rascunho, Fechada -> Impressa
            migrationBuilder.Sql("UPDATE \"NotasFiscais\" SET \"Status\" = 'Rascunho' WHERE \"Status\" = 'Aberta'");
            migrationBuilder.Sql("UPDATE \"NotasFiscais\" SET \"Status\" = 'Impressa' WHERE \"Status\" = 'Fechada'");
        }
    }
}
