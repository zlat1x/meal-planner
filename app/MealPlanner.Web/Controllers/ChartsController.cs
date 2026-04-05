using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChartsController : ControllerBase
{
    private readonly MealPlannerDbContext _context;

    public ChartsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    private sealed class FoodsByCategoryDto
    {
        public string CategoryName { get; set; } = null!;
        public int Count { get; set; }
    }

    [HttpGet("foods-by-category")]
    public async Task<IActionResult> GetFoodsByCategory()
    {
        var data = await _context.Foods
            .GroupBy(x => x.Category)
            .Select(g => new FoodsByCategoryDto
            {
                CategoryName = g.Key.ToString(),
                Count = g.Count()
            })
            .OrderBy(x => x.CategoryName)
            .ToListAsync();

        return Ok(data);
    }
}