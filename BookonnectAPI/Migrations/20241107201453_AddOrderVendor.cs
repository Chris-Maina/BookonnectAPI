using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Orders_OrderID",
                table: "OrderItem");

            migrationBuilder.AlterColumn<int>(
                name: "OrderID",
                table: "OrderItem",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "OrderVendorID",
                table: "OrderItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OrderVender",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderID = table.Column<int>(type: "INTEGER", nullable: false),
                    VendorID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderVender", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OrderVender_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderVender_Users_VendorID",
                        column: x => x.VendorID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderVendorID",
                table: "OrderItem",
                column: "OrderVendorID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderVender_OrderID",
                table: "OrderVender",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderVender_VendorID",
                table: "OrderVender",
                column: "VendorID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_OrderVender_OrderVendorID",
                table: "OrderItem",
                column: "OrderVendorID",
                principalTable: "OrderVender",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Orders_OrderID",
                table: "OrderItem",
                column: "OrderID",
                principalTable: "Orders",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_OrderVender_OrderVendorID",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Orders_OrderID",
                table: "OrderItem");

            migrationBuilder.DropTable(
                name: "OrderVender");

            migrationBuilder.DropIndex(
                name: "IX_OrderItem_OrderVendorID",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "OrderVendorID",
                table: "OrderItem");

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
        }
    }
}
