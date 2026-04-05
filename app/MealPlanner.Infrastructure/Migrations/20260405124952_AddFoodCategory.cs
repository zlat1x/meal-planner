using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "food",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "food");
        }
    }
}
