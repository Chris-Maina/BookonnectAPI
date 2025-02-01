using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLogsAndQuantityProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<int>(
            //    name: "Quantity",
            //    table: "Books",
            //    type: "INTEGER",
            //    nullable: false,
            //    defaultValue: 1);

            //migrationBuilder.CreateTable(
            //    name: "InventoryLogs",
            //    columns: table => new
            //    {
            //        ID = table.Column<int>(type: "INTEGER", nullable: false)
            //            .Annotation("Sqlite:Autoincrement", true),
            //        Quantity = table.Column<int>(type: "INTEGER", nullable: false),
            //        DateTime = table.Column<DateTime>(type: "timestamp", nullable: false),
            //        Type = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
            //        BookID = table.Column<int>(type: "INTEGER", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_InventoryLogs", x => x.ID);
            //        table.ForeignKey(
            //            name: "FK_InventoryLogs_Books_BookID",
            //            column: x => x.BookID,
            //            principalTable: "Books",
            //            principalColumn: "ID",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_InventoryLogs_BookID",
            //    table: "InventoryLogs",
            //    column: "BookID");

            migrationBuilder.Sql(@"
                 INSERT INTO InventoryLogs(Quantity, BookID, DateTime)
                 SELECT Quantity, ID, NOW()
                 FROM Books;
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE * FROM InventoryLogs;
            ");

            //migrationBuilder.DropTable(
            //    name: "InventoryLogs");

            //migrationBuilder.DropColumn(
            //    name: "Quantity",
            //    table: "Books");
        }
    }
}
