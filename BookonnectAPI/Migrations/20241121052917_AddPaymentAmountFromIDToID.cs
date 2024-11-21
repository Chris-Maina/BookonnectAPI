using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAmountFromIDToID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_UserID",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Payments",
                newName: "ToID");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_UserID",
                table: "Payments",
                newName: "IX_Payments_ToID");

            migrationBuilder.AddColumn<float>(
                name: "Amount",
                table: "Payments",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<int>(
                name: "FromID",
                table: "Payments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_FromID",
                table: "Payments",
                column: "FromID");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_FromID",
                table: "Payments",
                column: "FromID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_ToID",
                table: "Payments",
                column: "ToID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_FromID",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_ToID",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_FromID",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FromID",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "ToID",
                table: "Payments",
                newName: "UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_ToID",
                table: "Payments",
                newName: "IX_Payments_UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_UserID",
                table: "Payments",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
