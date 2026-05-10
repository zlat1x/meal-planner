using MealPlanner.Api.Models;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopListsController : ControllerBase
{
    private readonly MealPlannerDbContext _context;

    public ShopListsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    [HttpGet("by-plan/{planId:guid}")]
    public async Task<ActionResult<List<ShoppingItemResponse>>> GetByPlan(Guid planId)
    {
        var list = await _context.ShopLists
            .Include(x => x.Items)
                .ThenInclude(x => x.Food)
            .Include(x => x.Items)
                .ThenInclude(x => x.QuantityUnit)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.PlanId == planId);

        if (list == null)
        {
            return NotFound();
        }

        var response = list.Items
            .OrderBy(x => x.Food.Name)
            .Select(x => new ShoppingItemResponse
            {
                FoodId = x.FoodId,
                FoodName = x.Food.Name,
                TotalQuantityValue = x.TotalQuantityValue,
                UnitName = x.QuantityUnit.Name
            })
            .ToList();

        return Ok(response);
    }
}
