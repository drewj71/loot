using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GimmeTheLoot.Web.Migrations
{
    /// <inheritdoc />
    public partial class TransactionMerchantName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MerchantName",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerchantName",
                table: "Transactions");
        }
    }
}
