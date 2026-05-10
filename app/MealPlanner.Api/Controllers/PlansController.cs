using MealPlanner.Api.Models;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlansController : ControllerBase
{
    private readonly MealPlannerDbContext _context;

    public PlansController(MealPlannerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<PlanSummaryResponse>>> Get(Guid? userId)
    {
        var query = _context.Plans
            .Include(x => x.User)
            .Include(x => x.Meals)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        var plans = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PlanSummaryResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.User.Name,
                Days = x.Days,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                MealsCount = x.Meals.Count
            })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlanDetailsResponse>> GetById(Guid id)
    {
        var plan = await _context.Plans
            .Include(x => x.User)
            .Include(x => x.Meals)
                .ThenInclude(x => x.Items)
                    .ThenInclude(x => x.Food)
            .Include(x => x.Meals)
                .ThenInclude(x => x.Items)
                    .ThenInclude(x => x.QuantityUnit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (plan == null)
        {
            return NotFound();
        }

        var response = new PlanDetailsResponse
        {
            Id = plan.Id,
            UserId = plan.UserId,
            UserName = plan.User.Name,
            Days = plan.Days,
            Status = plan.Status,
            CreatedAt = plan.CreatedAt,
            MealsCount = plan.Meals.Count,
            Meals = plan.Meals
                .OrderBy(x => x.DayNo)
                .ThenBy(x => x.MealNo)
                .Select(x => new PlanMealResponse
                {
                    Id = x.Id,
                    DayNo = x.DayNo,
                    MealNo = x.MealNo,
                    Name = x.Name,
                    Items = x.Items.Select(i => new PlanMealItemResponse
                    {
                        FoodId = i.FoodId,
                        FoodName = i.Food.Name,
                        QuantityValue = i.QuantityValue,
                        UnitName = i.QuantityUnit.Name
                    }).ToList()
                })
                .ToList()
        };

        return Ok(response);
    }
}
