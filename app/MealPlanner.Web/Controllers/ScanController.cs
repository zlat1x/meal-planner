using Microsoft.AspNetCore.Mvc;

namespace MealPlanner.Web.Controllers;

public class ScanController : Controller
{
    private readonly IConfiguration _configuration;

    public ScanController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        ViewBag.ApiBaseUrl = _configuration["Api:BaseUrl"] ?? "http://localhost:5003";

        return View();
    }
}
