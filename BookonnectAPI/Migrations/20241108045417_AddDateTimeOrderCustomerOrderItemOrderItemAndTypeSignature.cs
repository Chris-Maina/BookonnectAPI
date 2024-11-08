using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDateTimeOrderCustomerOrderItemOrderItemAndTypeSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_OrderVendor_OrderVendorID",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Orders_OrderID",
                table: "OrderItem");

            migrationBuilder.DropTable(
                name: "OrderVendor");

            migrationBuilder.RenameColumn(
                name: "OrderVendorID",
                table: "OrderItem",
                newName: "CustomerID");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_OrderVendorID",
                table: "OrderItem",
                newName: "IX_OrderItem_CustomerID");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTime",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "OrderID",
                table: "OrderItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Orders_OrderID",
                table: "OrderItem",
                column: "OrderID",
                principalTable: "Orders",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Users_CustomerID",
                table: "OrderItem",
                column: "CustomerID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Orders_OrderID",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Users_CustomerID",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "DateTime",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "CustomerID",
                table: "OrderItem",
                newName: "OrderVendorID");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_CustomerID",
                table: "OrderItem",
                newName: "IX_OrderItem_OrderVendorID");

            migrationBuilder.AlterColumn<int>(
                name: "OrderID",
                table: "OrderItem",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateTable(
                name: "OrderVendor",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderID = table.Column<int>(type: "INTEGER", nullable: false),
                    VendorID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderVendor", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OrderVendor_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderVendor_Users_VendorID",
                        column: x => x.VendorID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderVendor_OrderID",
                table: "OrderVendor",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderVendor_VendorID",
                table: "OrderVendor",
                column: "VendorID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_OrderVendor_OrderVendorID",
                table: "OrderItem",
                column: "OrderVendorID",
                principalTable: "OrderVendor",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Orders_OrderID",
                table: "OrderItem",
                column: "OrderID",
                principalTable: "Orders",
                principalColumn: "ID");
        }
    }
}
