using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GimmeTheLoot.Web.Migrations
{
    /// <inheritdoc />
    public partial class TransactionCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoURL",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransactionCategoryId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransactionCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Primary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Detailed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionCategory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionCategoryId",
                table: "Transactions",
                column: "TransactionCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_TransactionCategory_TransactionCategoryId",
                table: "Transactions",
                column: "TransactionCategoryId",
                principalTable: "TransactionCategory",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_TransactionCategory_TransactionCategoryId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "TransactionCategory");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransactionCategoryId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LogoURL",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransactionCategoryId",
                table: "Transactions");
        }
    }
}
