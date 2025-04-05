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

            migrationBuilder.DropPrimaryKey(
                name: "PK_Recommendation",
                table: "Recommendation");

            migrationBuilder.RenameTable(
                name: "Recommendation",
                newName: "Recommendations");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendation_UserID",
                table: "Recommendations",
                newName: "IX_Recommendations_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendation_BookID",
                table: "Recommendations",
                newName: "IX_Recommendations_BookID");


            migrationBuilder.AddPrimaryKey(
                name: "PK_Recommendations",
                table: "Recommendations",
                column: "ID");

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

            migrationBuilder.DropPrimaryKey(
                name: "PK_Recommendations",
                table: "Recommendations");

            migrationBuilder.RenameTable(
                name: "Recommendations",
                newName: "Recommendation");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendations_UserID",
                table: "Recommendation",
                newName: "IX_Recommendation_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendations_BookID",
                table: "Recommendation",
                newName: "IX_Recommendation_BookID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Recommendation",
                table: "Recommendation",
                column: "ID");

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
