using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnedAffiliateDetailsReviewModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "AffiliateDetails",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Link = table.Column<string>(type: "TEXT", nullable: false),
                    SourceID = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "VARCHAR(20)", nullable: false),
                    BookID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateDetails", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AffiliateDetails_Books_BookID",
                        column: x => x.BookID,
                        principalTable: "Books",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OwnedDetails",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VendorID = table.Column<int>(type: "INTEGER", nullable: false),
                    BookID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnedDetails", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OwnedDetails_Books_BookID",
                        column: x => x.BookID,
                        principalTable: "Books",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OwnedDetails_Users_VendorID",
                        column: x => x.VendorID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp", nullable: false),
                    UserID = table.Column<int>(type: "INTEGER", nullable: false),
                    BookID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Reviews_Books_BookID",
                        column: x => x.BookID,
                        principalTable: "Books",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateDetails_BookID",
                table: "AffiliateDetails",
                column: "BookID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnedDetails_BookID",
                table: "OwnedDetails",
                column: "BookID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnedDetails_VendorID",
                table: "OwnedDetails",
                column: "VendorID");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookID",
                table: "Reviews",
                column: "BookID");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserID",
                table: "Reviews",
                column: "UserID");

            // Add AUTO_INCREMENT. MySQL specific
            migrationBuilder.Sql(@"
                ALTER TABLE OwnedDetails MODIFY ID INT AUTO_INCREMENT PRIMARY KEY;
                ALTER TABLE AffiliateDetails MODIFY ID INT AUTO_INCREMENT PRIMARY KEY;
                ALTER TABLE Reviews MODIFY ID INT AUTO_INCREMENT PRIMARY KEY;
                ");

            // Script to update OwnedDetails with existing data
            migrationBuilder.Sql(@"
                INSERT INTO OwnedDetails (BookID, VendorID)
                SELECT ID, VendorID
                FROM Books
                WHERE VendorID IS NOT NULL;
                ");

            migrationBuilder.DropForeignKey(
               name: "FK_Books_Users_VendorID",
               table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_VendorID",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "VendorID",
                table: "Books");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VendorID",
                table: "Books",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Books_VendorID",
                table: "Books",
                column: "VendorID");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Users_VendorID",
                table: "Books",
                column: "VendorID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(@"
                UPDATE Books
                SET VendorID = (
                    SELECT VendorID
                    FROM OwnedDetails
                    WHERE OwnedDetails.BookID = Books.ID
                )
                WHERE EXISTS(SELECT 1 FROM OwnedDetails WHERE OwnedDetails.BookID = Books.ID);
            ");

            migrationBuilder.DropTable(
                name: "AffiliateDetails");

            migrationBuilder.DropTable(
                name: "OwnedDetails");

            migrationBuilder.DropTable(
                name: "Reviews");

            
        }
    }
}
