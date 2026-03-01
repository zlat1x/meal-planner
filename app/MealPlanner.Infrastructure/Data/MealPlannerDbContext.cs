using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Infrastructure.Data;

public class MealPlannerDbContext : DbContext
{
    public MealPlannerDbContext(DbContextOptions<MealPlannerDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Macro> Macros => Set<Macro>();
    public DbSet<Configuration> Configurations => Set<Configuration>();
    public DbSet<MacroLog> MacroLogs => Set<MacroLog>();
    public DbSet<Icon> Icons => Set<Icon>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<ShopList> ShopLists => Set<ShopList>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<ShopItem> ShopItems => Set<ShopItem>();
    public DbSet<Export> Exports => Set<Export>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // users
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Email).HasColumnName("email").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasIndex(x => x.Email).IsUnique();

            e.HasOne(x => x.Profile)
                .WithOne(x => x.User)
                .HasForeignKey<Profile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Configuration)
                .WithOne(x => x.User)
                .HasForeignKey<Configuration>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // profile
        modelBuilder.Entity<Profile>(e =>
        {
            e.ToTable("profile");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.WeightKg).HasColumnName("weight_kg");
            e.Property(x => x.Goal).HasColumnName("goal");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => x.UserId).IsUnique();
        });

        // macro
        modelBuilder.Entity<Macro>(e =>
        {
            e.ToTable("macro");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.ProteinG).HasColumnName("protein_g");
            e.Property(x => x.CarbsG).HasColumnName("carbs_g");
            e.Property(x => x.FatG).HasColumnName("fat_g");
            e.Property(x => x.Mode).HasColumnName("mode").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasIndex(x => x.UserId).HasDatabaseName("idx_macro_user_id");

            e.HasOne(x => x.User)
                .WithMany(x => x.Macros)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // configuration
        modelBuilder.Entity<Configuration>(e =>
        {
            e.ToTable("configuration");
            e.HasKey(x => x.UserId);

            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Lang).HasColumnName("lang").IsRequired();
            e.Property(x => x.MealsPerDay).HasColumnName("meals_per_day");
            e.Property(x => x.ActiveMacroId).HasColumnName("active_macro_id");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => x.ActiveMacroId).HasDatabaseName("idx_configuration_active_macro");

            e.HasOne(x => x.ActiveMacro)
                .WithMany()
                .HasForeignKey(x => x.ActiveMacroId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // macro_log
        modelBuilder.Entity<MacroLog>(e =>
        {
            e.ToTable("macro_log");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.MacroId).HasColumnName("macro_id");
            e.Property(x => x.Event).HasColumnName("event").IsRequired();
            e.Property(x => x.ProteinG).HasColumnName("protein_g");
            e.Property(x => x.CarbsG).HasColumnName("carbs_g");
            e.Property(x => x.FatG).HasColumnName("fat_g");
            e.Property(x => x.ChangedAt).HasColumnName("changed_at");

            e.HasIndex(x => x.UserId).HasDatabaseName("idx_macro_log_user_id");
            e.HasIndex(x => x.MacroId).HasDatabaseName("idx_macro_log_macro_id");

            e.HasOne(x => x.User)
                .WithMany(x => x.MacroLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Macro)
                .WithMany()
                .HasForeignKey(x => x.MacroId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // icon
        modelBuilder.Entity<Icon>(e =>
        {
            e.ToTable("icon");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Code).HasColumnName("code").IsRequired();
            e.Property(x => x.Emoji).HasColumnName("emoji").IsRequired();

            e.HasIndex(x => x.Code).IsUnique();
        });

        // unit
        modelBuilder.Entity<Unit>(e =>
        {
            e.ToTable("unit");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Code).HasColumnName("code").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Kind).HasColumnName("kind").IsRequired();

            e.HasIndex(x => x.Code).IsUnique();
        });

        // food
        modelBuilder.Entity<Food>(e =>
        {
            e.ToTable("food");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.IconId).HasColumnName("icon_id");
            e.Property(x => x.Per100UnitId).HasColumnName("per_100_unit_id");

            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.ProteinPer100).HasColumnName("protein_per_100");
            e.Property(x => x.CarbsPer100).HasColumnName("carbs_per_100");
            e.Property(x => x.FatPer100).HasColumnName("fat_per_100");
            e.Property(x => x.KcalPer100).HasColumnName("kcal_per_100");
            e.Property(x => x.IsCustom).HasColumnName("is_custom");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => x.UserId).HasDatabaseName("idx_food_user_id");
            e.HasIndex(x => x.IconId).HasDatabaseName("idx_food_icon_id");
            e.HasIndex(x => x.Per100UnitId).HasDatabaseName("idx_food_per_100_unit_id");

            e.HasOne(x => x.User)
                .WithMany(x => x.Foods)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Icon)
                .WithMany(x => x.Foods)
                .HasForeignKey(x => x.IconId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Per100Unit)
                .WithMany(x => x.FoodsPer100Unit)
                .HasForeignKey(x => x.Per100UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // plan
        modelBuilder.Entity<Plan>(e =>
        {
            e.ToTable("plan");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.MacroId).HasColumnName("macro_id");
            e.Property(x => x.Days).HasColumnName("days");
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasIndex(x => x.UserId).HasDatabaseName("idx_plan_user_id");
            e.HasIndex(x => x.MacroId).HasDatabaseName("idx_plan_macro_id");

            e.HasOne(x => x.User)
                .WithMany(x => x.Plans)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Macro)
                .WithMany(x => x.Plans)
                .HasForeignKey(x => x.MacroId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // shop_list
        modelBuilder.Entity<ShopList>(e =>
        {
            e.ToTable("shop_list");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PlanId).HasColumnName("plan_id");
            e.Property(x => x.Days).HasColumnName("days");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasIndex(x => x.PlanId).HasDatabaseName("idx_shop_list_plan_id");

            e.HasOne(x => x.Plan)
                .WithMany(x => x.ShopLists)
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // meal
        modelBuilder.Entity<Meal>(e =>
        {
            e.ToTable("meal");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PlanId).HasColumnName("plan_id");
            e.Property(x => x.DayNo).HasColumnName("day_no");
            e.Property(x => x.MealNo).HasColumnName("meal_no");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();

            e.HasIndex(x => x.PlanId).HasDatabaseName("idx_meal_plan_id");

            e.HasOne(x => x.Plan)
                .WithMany(x => x.Meals)
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // meal_item
        modelBuilder.Entity<MealItem>(e =>
        {
            e.ToTable("meal_item");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.MealId).HasColumnName("meal_id");
            e.Property(x => x.FoodId).HasColumnName("food_id");
            e.Property(x => x.QuantityValue).HasColumnName("quantity_value");
            e.Property(x => x.QuantityUnitId).HasColumnName("quantity_unit_id");
            e.Property(x => x.Per100UnitId).HasColumnName("per_100_unit_id");
            e.Property(x => x.ProteinPer100).HasColumnName("protein_per_100");
            e.Property(x => x.CarbsPer100).HasColumnName("carbs_per_100");
            e.Property(x => x.FatPer100).HasColumnName("fat_per_100");
            e.Property(x => x.KcalPer100).HasColumnName("kcal_per_100");
            e.Property(x => x.IsLocked).HasColumnName("is_locked");

            e.HasIndex(x => x.MealId).HasDatabaseName("idx_meal_item_meal_id");
            e.HasIndex(x => x.FoodId).HasDatabaseName("idx_meal_item_food_id");
            e.HasIndex(x => x.QuantityUnitId).HasDatabaseName("idx_meal_item_quantity_unit_id");
            e.HasIndex(x => x.Per100UnitId).HasDatabaseName("idx_meal_item_per_100_unit_id");

            e.HasOne(x => x.Meal)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.MealId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Food)
                .WithMany(x => x.MealItems)
                .HasForeignKey(x => x.FoodId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.QuantityUnit)
                .WithMany(x => x.MealItemsQuantityUnit)
                .HasForeignKey(x => x.QuantityUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Per100Unit)
                .WithMany(x => x.MealItemsPer100Unit)
                .HasForeignKey(x => x.Per100UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // shop_item
        modelBuilder.Entity<ShopItem>(e =>
        {
            e.ToTable("shop_item");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ListId).HasColumnName("list_id");
            e.Property(x => x.FoodId).HasColumnName("food_id");
            e.Property(x => x.TotalQuantityValue).HasColumnName("total_quantity_value");
            e.Property(x => x.QuantityUnitId).HasColumnName("quantity_unit_id");

            e.HasIndex(x => x.ListId).HasDatabaseName("idx_shop_item_list_id");
            e.HasIndex(x => x.FoodId).HasDatabaseName("idx_shop_item_food_id");
            e.HasIndex(x => x.QuantityUnitId).HasDatabaseName("idx_shop_item_quantity_unit_id");

            e.HasOne(x => x.List)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Food)
                .WithMany(x => x.ShopItems)
                .HasForeignKey(x => x.FoodId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.QuantityUnit)
                .WithMany(x => x.ShopItemsQuantityUnit)
                .HasForeignKey(x => x.QuantityUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // export
        modelBuilder.Entity<Export>(e =>
        {
            e.ToTable("export");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Type).HasColumnName("type").IsRequired();
            e.Property(x => x.PlanId).HasColumnName("plan_id");
            e.Property(x => x.ListId).HasColumnName("list_id");
            e.Property(x => x.FileUrl).HasColumnName("file_url").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasIndex(x => x.UserId).HasDatabaseName("idx_export_user_id");

            e.HasOne(x => x.User)
                .WithMany(x => x.Exports)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Plan)
                .WithMany(x => x.Exports)
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.List)
                .WithMany(x => x.Exports)
                .HasForeignKey(x => x.ListId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}