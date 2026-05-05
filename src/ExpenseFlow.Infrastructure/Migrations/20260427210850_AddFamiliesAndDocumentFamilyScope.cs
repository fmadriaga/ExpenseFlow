using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFamiliesAndDocumentFamilyScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_FileHash",
                table: "Documents");

            migrationBuilder.CreateTable(
                name: "Families",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    InboxPath = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedPath = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorPath = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Families",
                columns: new[] { "Id", "Name", "InboxPath", "ProcessedPath", "ErrorPath" },
                values: new object[]
                {
                    1,
                    "Default",
                    "../../storage/familia/inbox",
                    "../../storage/familia/processed",
                    "../../storage/familia/error",
                });

            migrationBuilder.InsertData(
                table: "Families",
                columns: new[] { "Id", "Name", "InboxPath", "ProcessedPath", "ErrorPath" },
                values: new object[]
                {
                    2,
                    "Familia 2",
                    "../../storage/familia2/inbox",
                    "../../storage/familia2/processed",
                    "../../storage/familia2/error",
                });

            migrationBuilder.AddColumn<int>(
                name: "FamilyId",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FamilyId_FileHash",
                table: "Documents",
                columns: new[] { "FamilyId", "FileHash" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Families_FamilyId",
                table: "Documents",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Families_FamilyId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_FamilyId_FileHash",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "Families");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FileHash",
                table: "Documents",
                column: "FileHash",
                unique: true);
        }
    }
}
