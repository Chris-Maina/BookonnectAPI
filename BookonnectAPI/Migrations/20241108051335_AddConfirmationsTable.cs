using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddConfirmationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Confirmations",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderItemID = table.Column<int>(type: "INTEGER", nullable: false),
                    UserID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Confirmations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Confirmations_OrderItem_OrderItemID",
                        column: x => x.OrderItemID,
                        principalTable: "OrderItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Confirmations_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Confirmations_OrderItemID",
                table: "Confirmations",
                column: "OrderItemID");

            migrationBuilder.CreateIndex(
                name: "IX_Confirmations_UserID",
                table: "Confirmations",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Confirmations");
        }
    }
}
