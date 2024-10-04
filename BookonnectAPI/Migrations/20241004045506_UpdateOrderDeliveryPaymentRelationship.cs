using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderDeliveryPaymentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Deliveries_DeliveryID",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Payments_PaymentID",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentID",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentID",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "OrderID",
                table: "Payments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "DeliveryID",
                table: "Orders",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderID",
                table: "Payments",
                column: "OrderID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Deliveries_DeliveryID",
                table: "Orders",
                column: "DeliveryID",
                principalTable: "Deliveries",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Orders_OrderID",
                table: "Payments",
                column: "OrderID",
                principalTable: "Orders",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Deliveries_DeliveryID",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Orders_OrderID",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderID",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OrderID",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "DeliveryID",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentID",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentID",
                table: "Orders",
                column: "PaymentID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Deliveries_DeliveryID",
                table: "Orders",
                column: "DeliveryID",
                principalTable: "Deliveries",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Payments_PaymentID",
                table: "Orders",
                column: "PaymentID",
                principalTable: "Payments",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
