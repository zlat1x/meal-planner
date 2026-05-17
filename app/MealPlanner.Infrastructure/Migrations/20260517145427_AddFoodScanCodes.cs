using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodScanCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "food_scan_code",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    food_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    code_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_food_scan_code", x => x.id);
                    table.ForeignKey(
                        name: "FK_food_scan_code_food_food_id",
                        column: x => x.food_id,
                        principalTable: "food",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_food_scan_code_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "food_scan_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    food_scan_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    food_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scanned_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    scanned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_food_scan_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_food_scan_log_food_food_id",
                        column: x => x.food_id,
                        principalTable: "food",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_food_scan_log_food_scan_code_food_scan_code_id",
                        column: x => x.food_scan_code_id,
                        principalTable: "food_scan_code",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_food_scan_log_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_food_scan_code_food_id",
                table: "food_scan_code",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "idx_food_scan_code_user_id",
                table: "food_scan_code",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_food_scan_code_code_value",
                table: "food_scan_code",
                column: "code_value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_food_scan_log_code_id",
                table: "food_scan_log",
                column: "food_scan_code_id");

            migrationBuilder.CreateIndex(
                name: "idx_food_scan_log_food_id",
                table: "food_scan_log",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "idx_food_scan_log_user_id",
                table: "food_scan_log",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "food_scan_log");

            migrationBuilder.DropTable(
                name: "food_scan_code");
        }
    }
}
