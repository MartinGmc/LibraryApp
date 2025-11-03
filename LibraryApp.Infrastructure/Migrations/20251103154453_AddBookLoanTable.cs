using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookLoanTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookLoans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BookId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    BorrowedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReturnedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookLoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookLoans_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookLoans_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookLoans_BookId",
                table: "BookLoans",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookLoans_BookId_ReturnedDate",
                table: "BookLoans",
                columns: new[] { "BookId", "ReturnedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BookLoans_UserId",
                table: "BookLoans",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookLoans");
        }
    }
}
