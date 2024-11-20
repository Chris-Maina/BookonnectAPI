using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsUniqueOnBookID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_BookID",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_BookID",
                table: "OrderItems",
                column: "BookID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_BookID",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_BookID",
                table: "OrderItems",
                column: "BookID",
                unique: true);
        }
    }
}
