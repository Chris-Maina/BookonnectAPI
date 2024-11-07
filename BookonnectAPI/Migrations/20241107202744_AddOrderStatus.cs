using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_OrderVender_OrderVendorID",
                table: "OrderItem");

            migrationBuilder.DropTable(
                name: "OrderVender");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_OrderVendor_OrderVendorID",
                table: "OrderItem");

            migrationBuilder.DropTable(
                name: "OrderVendor");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Orders",
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
        }
    }
}
