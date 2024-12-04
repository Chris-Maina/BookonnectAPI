using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class ModifyPaymentID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ID",
                table: "Payments",
                type: "VARCHAR",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "ID",
                keyValue: 1,
                column: "Email",
                value: "mainachrisw@gmail.com");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ID",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "VARCHAR",
                oldMaxLength: 10);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "ID",
                keyValue: 1,
                column: "Email",
                value: "chris@bookonnect.com");
        }
    }
}
