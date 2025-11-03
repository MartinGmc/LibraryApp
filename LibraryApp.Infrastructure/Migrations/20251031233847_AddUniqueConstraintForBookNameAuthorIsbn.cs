using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintForBookNameAuthorIsbn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Books_Name_Author_ISBN",
                table: "Books",
                columns: new[] { "Name", "Author", "ISBN" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_Name_Author_ISBN",
                table: "Books");
        }
    }
}
