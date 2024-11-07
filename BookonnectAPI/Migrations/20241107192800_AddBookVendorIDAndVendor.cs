using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBookVendorIDAndVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Books_Users_UserID",
                table: "Books");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Books",
                newName: "VendorID");

            migrationBuilder.RenameIndex(
                name: "IX_Books_UserID",
                table: "Books",
                newName: "IX_Books_VendorID");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Users_VendorID",
                table: "Books",
                column: "VendorID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Books_Users_VendorID",
                table: "Books");

            migrationBuilder.RenameColumn(
                name: "VendorID",
                table: "Books",
                newName: "UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Books_VendorID",
                table: "Books",
                newName: "IX_Books_UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Users_UserID",
                table: "Books",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
