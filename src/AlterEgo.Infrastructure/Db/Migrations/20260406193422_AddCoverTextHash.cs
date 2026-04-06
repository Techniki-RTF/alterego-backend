using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlterEgo.Infrastructure.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverTextHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cover_text_hash",
                table: "Messages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_dialog_id_sender_telegram_id_cover_text_hash",
                table: "Messages",
                columns: new[] { "dialog_id", "sender_telegram_id", "cover_text_hash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_dialog_id_sender_telegram_id_cover_text_hash",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "cover_text_hash",
                table: "Messages");
        }
    }
}
