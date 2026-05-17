using MealPlanner.Api.Controllers;
using MealPlanner.Api.Models;
using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MealPlanner.Api.Tests;

[TestClass]
public class FoodsControllerTests
{
    [TestMethod]
    public async Task Get_ReturnsFoodsFilteredByCategory()
    {
        var context = CreateContext();
        SeedBaseData(context);

        var controller = new FoodsController(context);

        var result = await controller.Get(null, FoodCategory.Protein);

        var okResult = result.Result as OkObjectResult;
        var foods = okResult?.Value as List<FoodResponse>;

        Assert.IsNotNull(okResult);
        Assert.IsNotNull(foods);
        Assert.AreEqual(1, foods.Count);
        Assert.AreEqual("Куряче філе", foods[0].Name);
    }

    [TestMethod]
    public async Task Create_WithCorrectData_AddsFood()
    {
        var context = CreateContext();
        var data = SeedBaseData(context);

        var controller = new FoodsController(context);

        var request = new CreateFoodRequest
        {
            UserId = data.UserId,
            Per100UnitId = data.UnitId,
            Name = "Грецький йогурт",
            Category = FoodCategory.Protein,
            ProteinPer100 = 10,
            CarbsPer100 = 4,
            FatPer100 = 2,
            KcalPer100 = 74
        };

        var result = await controller.Create(request);

        var createdResult = result.Result as CreatedAtActionResult;

        Assert.IsNotNull(createdResult);
        Assert.AreEqual(3, await context.Foods.CountAsync());
    }

    private static MealPlannerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MealPlannerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MealPlannerDbContext(options);
    }

    private static TestData SeedBaseData(MealPlannerDbContext context)
    {
        var userId = Guid.NewGuid();
        var unitId = Guid.NewGuid();

        context.Users.Add(new User
        {
            Id = userId,
            Email = "test@example.com",
            Name = "Тестовий користувач",
            CreatedAt = DateTime.UtcNow
        });

        context.Units.Add(new Unit
        {
            Id = unitId,
            Code = "g",
            Name = "г",
            Kind = "mass"
        });

        context.Foods.Add(new Food
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Per100UnitId = unitId,
            Name = "Куряче філе",
            Category = FoodCategory.Protein,
            ProteinPer100 = 23,
            CarbsPer100 = 0,
            FatPer100 = 2,
            KcalPer100 = 110,
            IsCustom = false,
            UpdatedAt = DateTime.UtcNow
        });

        context.Foods.Add(new Food
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Per100UnitId = unitId,
            Name = "Рис",
            Category = FoodCategory.Carb,
            ProteinPer100 = 7,
            CarbsPer100 = 78,
            FatPer100 = 1,
            KcalPer100 = 350,
            IsCustom = false,
            UpdatedAt = DateTime.UtcNow
        });

        context.SaveChanges();

        return new TestData(userId, unitId);
    }

    private record TestData(Guid UserId, Guid UnitId);
}
