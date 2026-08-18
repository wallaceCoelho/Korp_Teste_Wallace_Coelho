using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Products",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "portuguese")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Description", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SearchVector",
                table: "Products",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_SearchVector",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Products");
        }
    }
}
