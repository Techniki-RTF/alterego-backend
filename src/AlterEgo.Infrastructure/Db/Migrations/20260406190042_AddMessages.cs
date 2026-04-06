using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlterEgo.Infrastructure.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    telegram_message_id = table.Column<long>(type: "bigint", nullable: false),
                    sender_telegram_id = table.Column<long>(type: "bigint", nullable: false),
                    dialog_id = table.Column<long>(type: "bigint", nullable: false),
                    original_text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_dialog_id",
                table: "Messages",
                column: "dialog_id");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_dialog_id_telegram_message_id",
                table: "Messages",
                columns: new[] { "dialog_id", "telegram_message_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");
        }
    }
}
