using MealPlanner.Api.Models;
using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FoodsController : ControllerBase
{
    private readonly MealPlannerDbContext _context;

    public FoodsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<FoodResponse>>> Get(string? search, FoodCategory? category)
    {
        var query = _context.Foods
            .Include(x => x.Per100Unit)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(normalizedSearch));
        }

        if (category.HasValue)
        {
            query = query.Where(x => x.Category == category.Value);
        }

        var foods = await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .Select(x => ToResponse(x))
            .ToListAsync();

        return Ok(foods);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FoodResponse>> GetById(Guid id)
    {
        var food = await _context.Foods
            .Include(x => x.Per100Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (food == null)
        {
            return NotFound();
        }

        return Ok(ToResponse(food));
    }

    [HttpPost]
    public async Task<ActionResult<FoodResponse>> Create(CreateFoodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Food name is required.");
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == request.UserId);
        var unitExists = await _context.Units.AnyAsync(x => x.Id == request.Per100UnitId);

        if (!userExists)
        {
            return BadRequest("Selected user does not exist.");
        }

        if (!unitExists)
        {
            return BadRequest("Selected unit does not exist.");
        }

        var food = new Food
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            IconId = request.IconId,
            Per100UnitId = request.Per100UnitId,
            Name = request.Name.Trim(),
            Category = request.Category,
            ProteinPer100 = request.ProteinPer100,
            CarbsPer100 = request.CarbsPer100,
            FatPer100 = request.FatPer100,
            KcalPer100 = request.KcalPer100,
            IsCustom = true,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Foods.Add(food);
        await _context.SaveChangesAsync();

        await _context.Entry(food).Reference(x => x.Per100Unit).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = food.Id }, ToResponse(food));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateFoodRequest request)
    {
        var food = await _context.Foods.FindAsync(id);

        if (food == null)
        {
            return NotFound();
        }

        if (request.Per100UnitId.HasValue)
        {
            var unitExists = await _context.Units.AnyAsync(x => x.Id == request.Per100UnitId.Value);

            if (!unitExists)
            {
                return BadRequest("Selected unit does not exist.");
            }

            food.Per100UnitId = request.Per100UnitId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            food.Name = request.Name.Trim();
        }

        if (request.Category.HasValue)
        {
            food.Category = request.Category.Value;
        }

        if (request.ProteinPer100.HasValue)
        {
            food.ProteinPer100 = request.ProteinPer100.Value;
        }

        if (request.CarbsPer100.HasValue)
        {
            food.CarbsPer100 = request.CarbsPer100.Value;
        }

        if (request.FatPer100.HasValue)
        {
            food.FatPer100 = request.FatPer100.Value;
        }

        if (request.KcalPer100.HasValue)
        {
            food.KcalPer100 = request.KcalPer100.Value;
        }

        if (request.IconId.HasValue)
        {
            food.IconId = request.IconId;
        }

        food.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var food = await _context.Foods.FindAsync(id);

        if (food == null)
        {
            return NotFound();
        }

        _context.Foods.Remove(food);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static FoodResponse ToResponse(Food food)
    {
        return new FoodResponse
        {
            Id = food.Id,
            UserId = food.UserId,
            Name = food.Name,
            Category = food.Category,
            ProteinPer100 = food.ProteinPer100,
            CarbsPer100 = food.CarbsPer100,
            FatPer100 = food.FatPer100,
            KcalPer100 = food.KcalPer100,
            UnitCode = food.Per100Unit.Code,
            UnitName = food.Per100Unit.Name,
            IsCustom = food.IsCustom,
            UpdatedAt = food.UpdatedAt
        };
    }
}
