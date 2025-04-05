using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookonnectAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenameRecommendationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recommendation_Books_BookID",
                table: "Recommendation");

            migrationBuilder.DropForeignKey(
                name: "FK_Recommendation_Users_UserID",
                table: "Recommendation");

            /**
             * MSQL version limitation in Production affects the order of rename operation and renaming primary key.
             * The limitation arises when a table exists without any primary key after a DROP PRIMARY KEY statement. 
             * By performing the RENAME TABLE before the DROP PRIMARY KEY, the table briefly exists under the new name with the old primary key. 
             * Then, we immediately drop and add the new primary key to the table with its new name.
             * 
             * Therefore steps include
             * 1. Rename table comes first
             * 2. Droping old primary key in new table name i.e. ALTER TABLE `Recommendations` DROP PRIMARY KEY;
             * 3. Add PK in new table name i.e. ALTER TABLE `Recommendations` ADD PRIMARY KEY (`ID`);
             */
            //migrationBuilder.DropPrimaryKey(
            //    name: "PK_Recommendation",
            //    table: "Recommendation");

            migrationBuilder.RenameTable(
                name: "Recommendation",
                newName: "Recommendations");

            // Using Sql() in Production due to lack of privileges instead of DropPrimaryKey
            migrationBuilder.Sql("ALTER TABLE `Recommendations` DROP PRIMARY KEY, ADD PRIMARY KEY (`ID`);");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendation_UserID",
                table: "Recommendations",
                newName: "IX_Recommendations_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendation_BookID",
                table: "Recommendations",
                newName: "IX_Recommendations_BookID");

            //migrationBuilder.AddPrimaryKey(
            //    name: "PK_Recommendations",
            //    table: "Recommendations",
            //    column: "ID");

        migrationBuilder.AddForeignKey(
                name: "FK_Recommendations_Books_BookID",
                table: "Recommendations",
                column: "BookID",
                principalTable: "Books",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recommendations_Users_UserID",
                table: "Recommendations",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recommendations_Books_BookID",
                table: "Recommendations");

            migrationBuilder.DropForeignKey(
                name: "FK_Recommendations_Users_UserID",
                table: "Recommendations");

            //migrationBuilder.DropPrimaryKey(
            //    name: "PK_Recommendations",
            //    table: "Recommendations");

            migrationBuilder.RenameTable(
                name: "Recommendations",
                newName: "Recommendation");

            migrationBuilder.Sql("ALTER TABLE `Recommendation` DROP PRIMARY KEY, ADD PRIMARY KEY (`ID`);");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendations_UserID",
                table: "Recommendation",
                newName: "IX_Recommendation_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendations_BookID",
                table: "Recommendation",
                newName: "IX_Recommendation_BookID");

            //migrationBuilder.AddPrimaryKey(
            //    name: "PK_Recommendation",
            //    table: "Recommendation",
            //    column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Recommendation_Books_BookID",
                table: "Recommendation",
                column: "BookID",
                principalTable: "Books",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recommendation_Users_UserID",
                table: "Recommendation",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
