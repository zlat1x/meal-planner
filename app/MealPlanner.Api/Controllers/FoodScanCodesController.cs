using MealPlanner.Api.Models;
using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/food-scan-codes")]
public class FoodScanCodesController : ControllerBase
{
    private readonly MealPlannerDbContext _context;

    public FoodScanCodesController(MealPlannerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<FoodScanCodeResponse>>> Get()
    {
        var codes = await _context.Set<FoodScanCode>()
            .Include(x => x.Food)
            .Include(x => x.User)
            .OrderBy(x => x.Food.Name)
            .Select(x => ToResponse(x))
            .ToListAsync();

        return Ok(codes);
    }

    [HttpPost]
    public async Task<ActionResult<FoodScanCodeResponse>> Create(CreateFoodScanCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CodeValue))
        {
            return BadRequest("Code value is required.");
        }

        var food = await _context.Foods.FirstOrDefaultAsync(x => x.Id == request.FoodId);
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId);

        if (food == null)
        {
            return BadRequest("Selected food does not exist.");
        }

        if (user == null)
        {
            return BadRequest("Selected user does not exist.");
        }

        var normalizedCode = request.CodeValue.Trim();

        var codeExists = await _context.Set<FoodScanCode>()
            .AnyAsync(x => x.CodeValue == normalizedCode);

        if (codeExists)
        {
            return BadRequest("This code is already connected to another product.");
        }

        var scanCode = new FoodScanCode
        {
            Id = Guid.NewGuid(),
            FoodId = food.Id,
            UserId = user.Id,
            CodeValue = normalizedCode,
            CodeType = string.IsNullOrWhiteSpace(request.CodeType) ? "Barcode" : request.CodeType.Trim(),
            Note = request.Note,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<FoodScanCode>().Add(scanCode);
        await _context.SaveChangesAsync();

        scanCode.Food = food;
        scanCode.User = user;

        return CreatedAtAction(nameof(Resolve), new { codeValue = scanCode.CodeValue }, ToResponse(scanCode));
    }

    [HttpGet("resolve/{codeValue}")]
    public async Task<ActionResult<ScanFoodCodeResponse>> Resolve(string codeValue)
    {
        var scanCode = await _context.Set<FoodScanCode>()
            .Include(x => x.Food)
            .FirstOrDefaultAsync(x => x.CodeValue == codeValue.Trim());

        if (scanCode == null)
        {
            return NotFound(new ScanFoodCodeResponse
            {
                Found = false,
                Message = "Product was not found by this code."
            });
        }

        return Ok(ToScanResponse(scanCode));
    }

    [HttpPost("scan")]
    public async Task<ActionResult<ScanFoodCodeResponse>> Scan(ScanFoodCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CodeValue))
        {
            return BadRequest(new ScanFoodCodeResponse
            {
                Found = false,
                Message = "Code value is required."
            });
        }

        var normalizedCode = request.CodeValue.Trim();

        var scanCode = await _context.Set<FoodScanCode>()
            .Include(x => x.Food)
            .FirstOrDefaultAsync(x => x.CodeValue == normalizedCode);

        var log = new FoodScanLog
        {
            Id = Guid.NewGuid(),
            FoodScanCodeId = scanCode?.Id,
            FoodId = scanCode?.FoodId,
            UserId = request.UserId,
            ScannedCode = normalizedCode,
            Result = scanCode == null ? "Not found" : "Found",
            Source = string.IsNullOrWhiteSpace(request.Source) ? "Web camera" : request.Source.Trim(),
            ScannedAt = DateTime.UtcNow
        };

        _context.Set<FoodScanLog>().Add(log);
        await _context.SaveChangesAsync();

        if (scanCode == null)
        {
            return NotFound(new ScanFoodCodeResponse
            {
                Found = false,
                Message = "Product was not found by this code."
            });
        }

        return Ok(ToScanResponse(scanCode));
    }

    [HttpGet("logs")]
    public async Task<ActionResult<List<FoodScanLogResponse>>> GetLogs()
    {
        var logs = await _context.Set<FoodScanLog>()
            .Include(x => x.Food)
            .Include(x => x.User)
            .OrderByDescending(x => x.ScannedAt)
            .Select(x => new FoodScanLogResponse
            {
                Id = x.Id,
                ScannedCode = x.ScannedCode,
                Result = x.Result,
                Source = x.Source,
                FoodName = x.Food == null ? null : x.Food.Name,
                UserName = x.User == null ? null : x.User.Name,
                ScannedAt = x.ScannedAt
            })
            .ToListAsync();

        return Ok(logs);
    }

    private static FoodScanCodeResponse ToResponse(FoodScanCode scanCode)
    {
        return new FoodScanCodeResponse
        {
            Id = scanCode.Id,
            FoodId = scanCode.FoodId,
            FoodName = scanCode.Food.Name,
            UserId = scanCode.UserId,
            UserName = scanCode.User.Name,
            CodeValue = scanCode.CodeValue,
            CodeType = scanCode.CodeType,
            Note = scanCode.Note,
            CreatedAt = scanCode.CreatedAt,
            UpdatedAt = scanCode.UpdatedAt
        };
    }

    private static ScanFoodCodeResponse ToScanResponse(FoodScanCode scanCode)
    {
        return new ScanFoodCodeResponse
        {
            Found = true,
            Message = "Product was found.",
            FoodId = scanCode.FoodId,
            FoodName = scanCode.Food.Name,
            Category = scanCode.Food.Category.ToString(),
            ProteinPer100 = scanCode.Food.ProteinPer100,
            CarbsPer100 = scanCode.Food.CarbsPer100,
            FatPer100 = scanCode.Food.FatPer100,
            KcalPer100 = scanCode.Food.KcalPer100
        };
    }
}
