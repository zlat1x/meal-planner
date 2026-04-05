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

    private sealed class MacrosByCategoryDto
    {
        public string CategoryName { get; set; } = null!;
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fat { get; set; }
    }

    // 1 ДІАГРАМА — кількість
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

    // 🔥 2 ДІАГРАМА — середні макроси
    [HttpGet("macros-by-category")]
    public async Task<IActionResult> GetMacrosByCategory()
    {
        var data = await _context.Foods
            .GroupBy(x => x.Category)
            .Select(g => new MacrosByCategoryDto
            {
                CategoryName = g.Key.ToString(),
                Protein = (double)g.Average(x => x.ProteinPer100),
                Carbs = (double)g.Average(x => x.CarbsPer100),
                Fat = (double)g.Average(x => x.FatPer100)
            })
            .OrderBy(x => x.CategoryName)
            .ToListAsync();

        return Ok(data);
    }
}