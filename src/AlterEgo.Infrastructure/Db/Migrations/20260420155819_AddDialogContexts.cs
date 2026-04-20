using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlterEgo.Infrastructure.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogContexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DialogContexts",
                columns: table => new
                {
                    dialog_id = table.Column<long>(type: "bigint", nullable: false),
                    context_notes = table.Column<string>(type: "text", nullable: false),
                    recent_cover_messages = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialogContexts", x => x.dialog_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DialogContexts");
        }
    }
}
