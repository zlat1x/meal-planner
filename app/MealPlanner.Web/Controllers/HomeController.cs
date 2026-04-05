using System.Diagnostics;
using MealPlanner.Infrastructure.Data;
using MealPlanner.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class HomeController : Controller
{
    private readonly MealPlannerDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(MealPlannerDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeDashboardViewModel
        {
            UsersCount = await _context.Users.AsNoTracking().CountAsync(),
            FoodsCount = await _context.Foods.AsNoTracking().CountAsync(),
            PlansCount = await _context.Plans.AsNoTracking().CountAsync(),
            ShopListsCount = await _context.ShopLists.AsNoTracking().CountAsync(),
            ExportsCount = await _context.Exports.AsNoTracking().CountAsync(),

            LatestFoods = await _context.Foods
                .AsNoTracking()
                .Include(x => x.Icon)
                .Include(x => x.Per100Unit)
                .OrderByDescending(x => x.UpdatedAt)
                .Take(5)
                .ToListAsync(),

            LatestPlans = await _context.Plans
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Macro)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync(),

            LatestShopLists = await _context.ShopLists
                .AsNoTracking()
                .Include(x => x.Plan)
                .ThenInclude(x => x.User)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync(),

            LatestExports = await _context.Exports
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Plan)
                .Include(x => x.List)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}