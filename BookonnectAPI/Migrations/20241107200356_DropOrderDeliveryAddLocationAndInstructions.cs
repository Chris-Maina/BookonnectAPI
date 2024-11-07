using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class DropOrderDeliveryAddLocationAndInstructions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Deliveries_DeliveryID",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryID",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryID",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryInstructions",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryLocation",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryInstructions",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryLocation",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryID",
                table: "Orders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryID",
                table: "Orders",
                column: "DeliveryID");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Deliveries_DeliveryID",
                table: "Orders",
                column: "DeliveryID",
                principalTable: "Deliveries",
                principalColumn: "ID");
        }
    }
}
