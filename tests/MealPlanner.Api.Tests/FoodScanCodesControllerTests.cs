using MealPlanner.Api.Controllers;
using MealPlanner.Api.Models;
using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Tests;

[TestClass]
public class FoodScanCodesControllerTests
{
    [TestMethod]
    public async Task Scan_WithExistingCode_ReturnsFoundProduct()
    {
        var context = CreateContext();
        var data = SeedData(context);

        var controller = new FoodScanCodesController(context);

        var result = await controller.Scan(new ScanFoodCodeRequest
        {
            CodeValue = data.CodeValue,
            Source = "Unit test"
        });

        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as ScanFoodCodeResponse;

        Assert.IsNotNull(okResult);
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Found);
        Assert.AreEqual("Куряче філе", response.FoodName);
    }

    [TestMethod]
    public async Task Scan_WithUnknownCode_CreatesNotFoundLog()
    {
        var context = CreateContext();
        SeedData(context);

        var controller = new FoodScanCodesController(context);

        var result = await controller.Scan(new ScanFoodCodeRequest
        {
            CodeValue = "unknown-code",
            Source = "Unit test"
        });

        var notFoundResult = result.Result as NotFoundObjectResult;
        var logsCount = await context.Set<FoodScanLog>().CountAsync();

        Assert.IsNotNull(notFoundResult);
        Assert.AreEqual(1, logsCount);
    }

    private static MealPlannerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MealPlannerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MealPlannerDbContext(options);
    }

    private static TestData SeedData(MealPlannerDbContext context)
    {
        var userId = Guid.NewGuid();
        var foodId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var codeValue = "482000000001";

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
            Id = foodId,
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

        context.Set<FoodScanCode>().Add(new FoodScanCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodId = foodId,
            CodeValue = codeValue,
            CodeType = "Barcode",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.SaveChanges();

        return new TestData(codeValue);
    }

    private record TestData(string CodeValue);
}
