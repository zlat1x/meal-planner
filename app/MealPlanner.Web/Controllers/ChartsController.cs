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

    private sealed class FoodsByUnitDto
    {
        public string UnitName { get; set; } = null!;
        public int Count { get; set; }
    }

    [HttpGet("foods-by-unit")]
    public async Task<IActionResult> GetFoodsByUnit()
    {
        var data = await _context.Foods
            .Include(x => x.Per100Unit)
            .GroupBy(x => x.Per100Unit.Name)
            .Select(g => new FoodsByUnitDto
            {
                UnitName = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.UnitName)
            .ToListAsync();

        return Ok(data);
    }
}