using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrderCustomerIDAndCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserID",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Orders",
                newName: "CustomerID");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_UserID",
                table: "Orders",
                newName: "IX_Orders_CustomerID");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_CustomerID",
                table: "Orders",
                column: "CustomerID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_CustomerID",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "CustomerID",
                table: "Orders",
                newName: "UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_CustomerID",
                table: "Orders",
                newName: "IX_Orders_UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserID",
                table: "Orders",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
