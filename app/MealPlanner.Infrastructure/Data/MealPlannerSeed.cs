using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Infrastructure.Data;

public static class MealPlannerSeed
{
    public static async Task SeedAsync(MealPlannerDbContext context)
    {
        await context.Database.MigrateAsync();

        if (!await context.Users.AnyAsync())
        {
            var demoUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "Demo User",
                Email = "demo@mealplanner.local",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(demoUser);

            var gramUnit = new Unit
            {
                Id = Guid.NewGuid(),
                Code = "gram",
                Kind = "weight",
                Name = "gram"
            };

            context.Units.Add(gramUnit);

            var icons = CreateIcons();
            context.Icons.AddRange(icons);

            await context.SaveChangesAsync();

            var activeMacro = new Macro
            {
                Id = Guid.NewGuid(),
                UserId = demoUser.Id,
                ProteinG = 200,
                CarbsG = 500,
                FatG = 100,
                Mode = "base",
                CreatedAt = DateTime.UtcNow
            };

            context.Macros.Add(activeMacro);

            var configuration = new Configuration
            {
                UserId = demoUser.Id,
                Lang = "uk",
                MealsPerDay = 3,
                ActiveMacroId = activeMacro.Id,
                UpdatedAt = DateTime.UtcNow
            };

            context.Configurations.Add(configuration);

            await context.SaveChangesAsync();

            var gramId = gramUnit.Id;
            var iconMap = icons.ToDictionary(x => x.Code, x => x.Id);

            var foods = CreateFoods(demoUser.Id, gramId, iconMap);
            context.Foods.AddRange(foods);

            await context.SaveChangesAsync();
        }
        else if (!await context.Foods.AnyAsync())
        {
            var demoUser = await context.Users.OrderBy(x => x.CreatedAt).FirstAsync();
            var unitMap = await CreateUnits(context); 
            var gramUnitId = unitMap["gram"];

            var icons = await context.Icons.ToListAsync();
            var iconMap = icons.ToDictionary(x => x.Code, x => x.Id);

            var foods = CreateFoods(demoUser.Id, gramUnitId, iconMap);
            context.Foods.AddRange(foods);

            await context.SaveChangesAsync();
        }
    }

    private static async Task<Dictionary<string, Guid>> CreateUnits(MealPlannerDbContext context)
    {
        if (await context.Units.AnyAsync())
        {
            return await context.Units.ToDictionaryAsync(x => x.Code, x => x.Id);
        }

        var units = new List<Unit>
        {
            new() { Id = Guid.NewGuid(), Code = "gram", Name = "г", Kind = "weight" },
            new() { Id = Guid.NewGuid(), Code = "ml", Name = "мл", Kind = "volume" },
            new() { Id = Guid.NewGuid(), Code = "piece", Name = "шт", Kind = "count" },
            new() { Id = Guid.NewGuid(), Code = "scoop", Name = "скуп", Kind = "count" }
        };

        context.Units.AddRange(units);
        await context.SaveChangesAsync();

        return units.ToDictionary(x => x.Code, x => x.Id);
    }

    private static List<Icon> CreateIcons()
    {
        return new List<Icon>
        {
            new() { Id = Guid.NewGuid(), Code = "rice", Emoji = "🍚" },
            new() { Id = Guid.NewGuid(), Code = "porridge", Emoji = "🥣" },
            new() { Id = Guid.NewGuid(), Code = "milk", Emoji = "🥛" },
            new() { Id = Guid.NewGuid(), Code = "corn", Emoji = "🌽" },
            new() { Id = Guid.NewGuid(), Code = "pasta", Emoji = "🍝" },
            new() { Id = Guid.NewGuid(), Code = "potato", Emoji = "🥔" },
            new() { Id = Guid.NewGuid(), Code = "sweet_potato", Emoji = "🍠" },
            new() { Id = Guid.NewGuid(), Code = "beans", Emoji = "🫘" },
            new() { Id = Guid.NewGuid(), Code = "soup", Emoji = "🍲" },
            new() { Id = Guid.NewGuid(), Code = "falafel", Emoji = "🧆" },
            new() { Id = Guid.NewGuid(), Code = "bread", Emoji = "🍞" },
            new() { Id = Guid.NewGuid(), Code = "wrap", Emoji = "🌯" },
            new() { Id = Guid.NewGuid(), Code = "cracker", Emoji = "🍘" },
            new() { Id = Guid.NewGuid(), Code = "salad", Emoji = "🥗" },
            new() { Id = Guid.NewGuid(), Code = "yellow", Emoji = "🟡" },
            new() { Id = Guid.NewGuid(), Code = "noodles", Emoji = "🍜" },
            new() { Id = Guid.NewGuid(), Code = "banana", Emoji = "🍌" },

            new() { Id = Guid.NewGuid(), Code = "empty", Emoji = "🚫" },
            new() { Id = Guid.NewGuid(), Code = "chicken", Emoji = "🍗" },
            new() { Id = Guid.NewGuid(), Code = "turkey", Emoji = "🍖" },
            new() { Id = Guid.NewGuid(), Code = "meat", Emoji = "🥩" },
            new() { Id = Guid.NewGuid(), Code = "skewer", Emoji = "🍢" },
            new() { Id = Guid.NewGuid(), Code = "fish", Emoji = "🐟" },
            new() { Id = Guid.NewGuid(), Code = "sushi", Emoji = "🍣" },
            new() { Id = Guid.NewGuid(), Code = "can", Emoji = "🥫" },
            new() { Id = Guid.NewGuid(), Code = "squid", Emoji = "🦑" },
            new() { Id = Guid.NewGuid(), Code = "shrimp", Emoji = "🦐" },
            new() { Id = Guid.NewGuid(), Code = "seafood", Emoji = "🐙" },
            new() { Id = Guid.NewGuid(), Code = "egg", Emoji = "🥚" },
            new() { Id = Guid.NewGuid(), Code = "omelette", Emoji = "🍳" },
            new() { Id = Guid.NewGuid(), Code = "curd", Emoji = "🥡" },
            new() { Id = Guid.NewGuid(), Code = "cheese", Emoji = "🧀" },
            new() { Id = Guid.NewGuid(), Code = "pizza", Emoji = "🍕" },
            new() { Id = Guid.NewGuid(), Code = "shake", Emoji = "🥤" },
            new() { Id = Guid.NewGuid(), Code = "rabbit", Emoji = "🐇" },

            new() { Id = Guid.NewGuid(), Code = "olive", Emoji = "🫒" },
            new() { Id = Guid.NewGuid(), Code = "sunflower", Emoji = "🌻" },
            new() { Id = Guid.NewGuid(), Code = "butter", Emoji = "🧈" },
            new() { Id = Guid.NewGuid(), Code = "flax", Emoji = "🌿" },
            new() { Id = Guid.NewGuid(), Code = "coconut", Emoji = "🥥" },
            new() { Id = Guid.NewGuid(), Code = "avocado", Emoji = "🥑" },
            new() { Id = Guid.NewGuid(), Code = "nuts", Emoji = "🥜" },
            new() { Id = Guid.NewGuid(), Code = "sauce", Emoji = "🍶" }
        };
    }

    private static List<Food> CreateFoods(Guid userId, Guid gramUnitId, Dictionary<string, Guid> iconMap)
    {
        var now = DateTime.UtcNow;
        var foods = new List<Food>();

        Guid? GetIconId(string iconCode)
        {
            if (iconMap.TryGetValue(iconCode, out var iconId))
            {
                return iconId;
            }

            return null;
        }

        void Add(string name, FoodCategory category, decimal p, decimal c, decimal f, string iconCode, bool isCustom = false)
        {
            var kcal = p * 4m + c * 4m + f * 9m;

            foods.Add(new Food
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IconId = GetIconId(iconCode),
                Per100UnitId = gramUnitId,
                Name = name,
                Category = category,
                ProteinPer100 = p,
                CarbsPer100 = c,
                FatPer100 = f,
                KcalPer100 = decimal.Round(kcal, 1),
                IsCustom = isCustom,
                UpdatedAt = now
            });
        }

        Add("Cream of Rice", FoodCategory.Carb, 7.0m, 75.0m, 5.0m, "rice");
        Add("Гречка", FoodCategory.Carb, 12.6m, 68.0m, 3.3m, "porridge");
        Add("Овсянка (вода)", FoodCategory.Carb, 12.0m, 60.0m, 6.0m, "porridge");
        Add("Овсянка (молоко)", FoodCategory.Carb, 17.8m, 69.4m, 11.0m, "milk");
        Add("Рис білий", FoodCategory.Carb, 7.0m, 79.0m, 0.6m, "rice");
        Add("Рис Басматі", FoodCategory.Carb, 7.5m, 78.0m, 0.5m, "rice");
        Add("Перловка", FoodCategory.Carb, 9.3m, 73.0m, 1.1m, "porridge");
        Add("Пшоно", FoodCategory.Carb, 11.5m, 69.0m, 3.3m, "porridge");
        Add("Кукурудзяна крупа", FoodCategory.Carb, 8.0m, 75.0m, 1.0m, "corn");
        Add("Макарони твердих сортів", FoodCategory.Carb, 12.0m, 71.5m, 1.1m, "pasta");
        Add("Булгур", FoodCategory.Carb, 12.3m, 58.0m, 1.3m, "salad");
        Add("Картопля (варена)", FoodCategory.Carb, 2.0m, 17.0m, 0.4m, "potato");
        Add("Картопля (печена)", FoodCategory.Carb, 2.5m, 21.0m, 0.5m, "potato");
        Add("Батат", FoodCategory.Carb, 1.6m, 20.0m, 0.1m, "sweet_potato");
        Add("Фасоль", FoodCategory.Carb, 24.0m, 60.0m, 1.0m, "beans");
        Add("Чечевиця", FoodCategory.Carb, 24.0m, 54.0m, 1.0m, "soup");
        Add("Нут", FoodCategory.Carb, 20.0m, 60.0m, 6.0m, "falafel");
        Add("Хліб цільнозерновий", FoodCategory.Carb, 9.0m, 45.0m, 1.5m, "bread");
        Add("Тости", FoodCategory.Carb, 8.0m, 49.0m, 1.0m, "bread");
        Add("Лаваш", FoodCategory.Carb, 9.0m, 56.0m, 1.0m, "wrap");
        Add("Хлібці", FoodCategory.Carb, 10.0m, 65.0m, 2.0m, "cracker");
        Add("Кіноа", FoodCategory.Carb, 14.0m, 64.0m, 6.0m, "salad");
        Add("Кус-кус", FoodCategory.Carb, 12.0m, 77.0m, 0.6m, "yellow");
        Add("Рисова локшина", FoodCategory.Carb, 4.0m, 80.0m, 0.5m, "noodles");
        Add("Банан", FoodCategory.Carb, 1.5m, 21.0m, 0.5m, "banana");

        Add("Без м’яса", FoodCategory.Protein, 0.0m, 0.0m, 0.0m, "empty");
        Add("Куряче філе", FoodCategory.Protein, 23.6m, 0.0m, 1.9m, "chicken");
        Add("Індичка", FoodCategory.Protein, 25.0m, 0.0m, 1.0m, "turkey");
        Add("Фарш курячий (філе)", FoodCategory.Protein, 21.0m, 0.0m, 2.5m, "meat");
        Add("Фарш індичий", FoodCategory.Protein, 20.0m, 0.0m, 8.0m, "meat");
        Add("Куряче стегно", FoodCategory.Protein, 19.0m, 0.0m, 10.0m, "chicken");
        Add("Курячі серця", FoodCategory.Protein, 16.0m, 0.0m, 10.0m, "skewer");
        Add("Куряча печінка", FoodCategory.Protein, 19.0m, 0.0m, 6.3m, "soup");
        Add("Печінка яловича", FoodCategory.Protein, 18.0m, 0.0m, 4.0m, "soup");
        Add("Яловичина пісна", FoodCategory.Protein, 22.0m, 0.0m, 7.0m, "meat");
        Add("Фарш яловичий", FoodCategory.Protein, 19.0m, 0.0m, 12.0m, "meat");
        Add("Кролик", FoodCategory.Protein, 21.0m, 0.0m, 8.0m, "rabbit");
        Add("Біла риба", FoodCategory.Protein, 17.5m, 0.0m, 1.2m, "fish");
        Add("Тилапія", FoodCategory.Protein, 20.0m, 0.0m, 1.7m, "fish");
        Add("Червона риба", FoodCategory.Protein, 20.0m, 0.0m, 13.0m, "sushi");
        Add("Тунець (консервований)", FoodCategory.Protein, 23.0m, 0.0m, 1.0m, "can");
        Add("Кальмари", FoodCategory.Protein, 18.0m, 0.0m, 2.2m, "squid");
        Add("Креветки", FoodCategory.Protein, 24.0m, 0.0m, 0.5m, "shrimp");
        Add("Морепродукти", FoodCategory.Protein, 15.0m, 0.0m, 1.0m, "seafood");
        Add("Яйця", FoodCategory.Protein, 12.5m, 0.0m, 11.0m, "egg");
        Add("Омлет", FoodCategory.Protein, 12.5m, 0.0m, 13.0m, "omelette");
        Add("Сир кисломолочний 5%", FoodCategory.Protein, 18.0m, 3.0m, 5.0m, "curd");
        Add("Сир кисломолочний 0%", FoodCategory.Protein, 22.0m, 3.0m, 0.5m, "curd");
        Add("Сир легкий", FoodCategory.Protein, 30.0m, 0.0m, 15.0m, "cheese");
        Add("Моцарела", FoodCategory.Protein, 22.0m, 0.0m, 22.0m, "pizza");
        Add("Протеїн (Whey)", FoodCategory.Protein, 80.0m, 5.0m, 2.0m, "shake");

        Add("Оливкова олія", FoodCategory.Fat, 0.0m, 0.0m, 99.8m, "olive");
        Add("Соняшникова олія", FoodCategory.Fat, 0.0m, 0.0m, 99.9m, "sunflower");
        Add("Вершкове масло", FoodCategory.Fat, 0.0m, 0.0m, 82.5m, "butter");
        Add("Лляна олія", FoodCategory.Fat, 0.0m, 0.0m, 99.8m, "flax");
        Add("Кокосова олія", FoodCategory.Fat, 0.0m, 0.0m, 99.0m, "coconut");
        Add("Сметана 15%", FoodCategory.Fat, 0.0m, 0.0m, 15.0m, "milk");
        Add("Сметана 20%", FoodCategory.Fat, 0.0m, 0.0m, 20.0m, "milk");
        Add("Авокадо", FoodCategory.Fat, 0.0m, 0.0m, 15.0m, "avocado");
        Add("Горіхи", FoodCategory.Fat, 0.0m, 0.0m, 55.0m, "nuts");
        Add("Арахісова паста", FoodCategory.Fat, 0.0m, 0.0m, 50.0m, "nuts");
        Add("Майонез", FoodCategory.Fat, 0.0m, 0.0m, 67.0m, "sauce");
        Add("Песто", FoodCategory.Fat, 0.0m, 0.0m, 45.0m, "salad");
        Add("Без масла", FoodCategory.Fat, 0.0m, 0.0m, 0.0m, "empty");

        return foods;
    }
}