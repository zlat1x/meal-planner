using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "icon",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    emoji = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_icon", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "food",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    icon_id = table.Column<Guid>(type: "uuid", nullable: true),
                    per_100_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    protein_per_100 = table.Column<decimal>(type: "numeric", nullable: false),
                    carbs_per_100 = table.Column<decimal>(type: "numeric", nullable: false),
                    fat_per_100 = table.Column<decimal>(type: "numeric", nullable: false),
                    kcal_per_100 = table.Column<decimal>(type: "numeric", nullable: false),
                    is_custom = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_food", x => x.id);
                    table.ForeignKey(
                        name: "FK_food_icon_icon_id",
                        column: x => x.icon_id,
                        principalTable: "icon",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_food_unit_per_100_unit_id",
                        column: x => x.per_100_unit_id,
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_food_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "macro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protein_g = table.Column<int>(type: "integer", nullable: false),
                    carbs_g = table.Column<int>(type: "integer", nullable: false),
                    fat_g = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_macro", x => x.id);
                    table.ForeignKey(
                        name: "FK_macro_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric", nullable: true),
                    goal = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile", x => x.id);
                    table.ForeignKey(
                        name: "FK_profile_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "configuration",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lang = table.Column<string>(type: "text", nullable: false),
                    meals_per_day = table.Column<int>(type: "integer", nullable: false),
                    active_macro_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuration", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_configuration_macro_active_macro_id",
                        column: x => x.active_macro_id,
                        principalTable: "macro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_configuration_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "macro_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    macro_id = table.Column<Guid>(type: "uuid", nullable: true),
                    @event = table.Column<string>(name: "event", type: "text", nullable: false),
                    protein_g = table.Column<int>(type: "integer", nullable: false),
                    carbs_g = table.Column<int>(type: "integer", nullable: false),
                    fat_g = table.Column<int>(type: "integer", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_macro_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_macro_log_macro_macro_id",
                        column: x => x.macro_id,
                        principalTable: "macro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_macro_log_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    macro_id = table.Column<Guid>(type: "uuid", nullable: true),
                    days = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan", x => x.id);
                    table.ForeignKey(
                        name: "FK_plan_macro_macro_id",
                        column: x => x.macro_id,
                        principalTable: "macro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_plan_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_no = table.Column<int>(type: "integer", nullable: false),
                    meal_no = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal", x => x.id);
                    table.ForeignKey(
                        name: "FK_meal_plan_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_list",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_list", x => x.id);
                    table.ForeignKey(
                        name: "FK_shop_list_plan_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    meal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    food_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_value = table.Column<decimal>(type: "numeric", nullable: false),
                    quantity_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    per_100_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protein_per_100 = table.Column<decimal>(type: "numeric", nullable: false),
                    carbs_per_100 = table.Column<decimal>(type: "numeric", nullable: false),
                    fat_per_100 = table.Column<decimal>(type: "numeric", nullable: false),
                    kcal_per_100 = table.Column<decimal>(type: "numeric", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_meal_item_food_food_id",
                        column: x => x.food_id,
                        principalTable: "food",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meal_item_meal_meal_id",
                        column: x => x.meal_id,
                        principalTable: "meal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_item_unit_per_100_unit_id",
                        column: x => x.per_100_unit_id,
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meal_item_unit_quantity_unit_id",
                        column: x => x.quantity_unit_id,
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "export",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_url = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export", x => x.id);
                    table.ForeignKey(
                        name: "FK_export_plan_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_export_shop_list_list_id",
                        column: x => x.list_id,
                        principalTable: "shop_list",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_export_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    food_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_quantity_value = table.Column<decimal>(type: "numeric", nullable: false),
                    quantity_unit_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_shop_item_food_food_id",
                        column: x => x.food_id,
                        principalTable: "food",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shop_item_shop_list_list_id",
                        column: x => x.list_id,
                        principalTable: "shop_list",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shop_item_unit_quantity_unit_id",
                        column: x => x.quantity_unit_id,
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_configuration_active_macro",
                table: "configuration",
                column: "active_macro_id");

            migrationBuilder.CreateIndex(
                name: "idx_export_user_id",
                table: "export",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_export_list_id",
                table: "export",
                column: "list_id");

            migrationBuilder.CreateIndex(
                name: "IX_export_plan_id",
                table: "export",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "idx_food_icon_id",
                table: "food",
                column: "icon_id");

            migrationBuilder.CreateIndex(
                name: "idx_food_per_100_unit_id",
                table: "food",
                column: "per_100_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_food_user_id",
                table: "food",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_icon_code",
                table: "icon",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_macro_user_id",
                table: "macro",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_macro_log_macro_id",
                table: "macro_log",
                column: "macro_id");

            migrationBuilder.CreateIndex(
                name: "idx_macro_log_user_id",
                table: "macro_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_meal_plan_id",
                table: "meal",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "idx_meal_item_food_id",
                table: "meal_item",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "idx_meal_item_meal_id",
                table: "meal_item",
                column: "meal_id");

            migrationBuilder.CreateIndex(
                name: "idx_meal_item_per_100_unit_id",
                table: "meal_item",
                column: "per_100_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_meal_item_quantity_unit_id",
                table: "meal_item",
                column: "quantity_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_plan_macro_id",
                table: "plan",
                column: "macro_id");

            migrationBuilder.CreateIndex(
                name: "idx_plan_user_id",
                table: "plan",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_profile_user_id",
                table: "profile",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_shop_item_food_id",
                table: "shop_item",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "idx_shop_item_list_id",
                table: "shop_item",
                column: "list_id");

            migrationBuilder.CreateIndex(
                name: "idx_shop_item_quantity_unit_id",
                table: "shop_item",
                column: "quantity_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_shop_list_plan_id",
                table: "shop_list",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_unit_code",
                table: "unit",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuration");

            migrationBuilder.DropTable(
                name: "export");

            migrationBuilder.DropTable(
                name: "macro_log");

            migrationBuilder.DropTable(
                name: "meal_item");

            migrationBuilder.DropTable(
                name: "profile");

            migrationBuilder.DropTable(
                name: "shop_item");

            migrationBuilder.DropTable(
                name: "meal");

            migrationBuilder.DropTable(
                name: "food");

            migrationBuilder.DropTable(
                name: "shop_list");

            migrationBuilder.DropTable(
                name: "icon");

            migrationBuilder.DropTable(
                name: "unit");

            migrationBuilder.DropTable(
                name: "plan");

            migrationBuilder.DropTable(
                name: "macro");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
