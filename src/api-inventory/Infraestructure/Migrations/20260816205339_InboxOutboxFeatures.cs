using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class InboxOutboxFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InboxDeadLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeadLetteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompensatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompensationError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxDeadLetters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxDeadLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeadLetteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompensatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompensationError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxDeadLetters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxDeadLetters_CompensatedAt_DeadLetteredAt",
                table: "InboxDeadLetters",
                columns: new[] { "CompensatedAt", "DeadLetteredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_RetryCount_ReceivedAt",
                table: "InboxMessages",
                columns: new[] { "RetryCount", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxDeadLetters_CompensatedAt_DeadLetteredAt",
                table: "OutboxDeadLetters",
                columns: new[] { "CompensatedAt", "DeadLetteredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_RetryCount_CreatedAt",
                table: "OutboxMessages",
                columns: new[] { "RetryCount", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxDeadLetters");

            migrationBuilder.DropTable(
                name: "InboxMessages");

            migrationBuilder.DropTable(
                name: "OutboxDeadLetters");

            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}
