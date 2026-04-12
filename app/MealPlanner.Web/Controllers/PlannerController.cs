using ClosedXML.Excel;
using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using MealPlanner.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace MealPlanner.Web.Controllers;

public class PlannerController : Controller
{
    private readonly MealPlannerDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public PlannerController(MealPlannerDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var model = new PlannerPageViewModel();

        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);

        if (TempData["PlannerSuccess"] is string message)
        {
            ViewBag.PlannerSuccess = message;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoadDefaults(PlannerPageViewModel model)
    {
        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);
        await ApplyUserDefaultsAsync(model);

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(PlannerPageViewModel model)
    {
        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);

        ValidatePlannerInput(model);

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        await BuildResultAsync(model);

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PlannerPageViewModel model)
    {
        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);

        ValidatePlannerInput(model);

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        await BuildResultAsync(model);

        var saveResult = await SavePlanAsync(model);

        model.IsSaved = true;
        model.SavedPlanId = saveResult.planId;
        model.SavedShopListId = saveResult.shopListId;

        TempData["PlannerSuccess"] = "План харчування та список покупок успішно збережено.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportText(PlannerPageViewModel model)
    {
        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);

        ValidatePlannerInput(model);

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        await BuildResultAsync(model);

        var exportsPath = Path.Combine(_environment.WebRootPath, "exports");
        Directory.CreateDirectory(exportsPath);

        var fileName = $"meal-plan-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        var filePath = Path.Combine(exportsPath, fileName);

        await System.IO.File.WriteAllTextAsync(filePath, model.ExportText, Encoding.UTF8);

        if (model.UserId.HasValue)
        {
            var export = new Export
            {
                Id = Guid.NewGuid(),
                UserId = model.UserId.Value,
                Type = "txt",
                PlanId = null,
                ListId = null,
                FileUrl = $"/exports/{fileName}",
                CreatedAt = DateTime.UtcNow
            };

            _context.Exports.Add(export);
            await _context.SaveChangesAsync();
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, "text/plain", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportExcel(PlannerPageViewModel model)
    {
        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);

        ValidatePlannerInput(model);

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        await BuildResultAsync(model);

        var exportsPath = Path.Combine(_environment.WebRootPath, "exports");
        Directory.CreateDirectory(exportsPath);

        var fileName = $"meal-plan-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
        var filePath = Path.Combine(exportsPath, fileName);

        using (var workbook = new XLWorkbook())
        {
            BuildSummarySheet(workbook, model);
            BuildMealsSheet(workbook, model);
            BuildShoppingListSheet(workbook, model);
            workbook.SaveAs(filePath);
        }

        if (model.UserId.HasValue)
        {
            var export = new Export
            {
                Id = Guid.NewGuid(),
                UserId = model.UserId.Value,
                Type = "xlsx",
                PlanId = null,
                ListId = null,
                FileUrl = $"/exports/{fileName}",
                CreatedAt = DateTime.UtcNow
            };

            _context.Exports.Add(export);
            await _context.SaveChangesAsync();
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(PlannerPageViewModel model, IFormFile? excelFile)
    {
        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);

        if (excelFile == null || excelFile.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Потрібно вибрати Excel-файл для імпорту.");
            return View("Index", model);
        }

        var extension = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            ModelState.AddModelError(string.Empty, "Підтримується лише формат .xlsx.");
            return View("Index", model);
        }

        try
        {
            using var stream = excelFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var sheet = workbook.Worksheets.FirstOrDefault(x => x.Name == "Список покупок")
                        ?? workbook.Worksheet(1);

            model.ImportedExcelItems = ReadImportedShoppingItems(sheet);
            model.ImportedExcelFileName = excelFile.FileName;

            if (!model.ImportedExcelItems.Any())
            {
                ModelState.AddModelError(string.Empty, "У файлі не знайдено жодної позиції для імпорту.");
                return View("Index", model);
            }

            ViewBag.ImportSuccess = $"Файл {excelFile.FileName} успішно імпортовано. Зчитано {model.ImportedExcelItems.Count} позицій.";

            return View("Index", model);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Не вдалося прочитати Excel-файл. Перевір формат і структуру файлу.");
            return View("Index", model);
        }
    }

    private void BuildSummarySheet(XLWorkbook workbook, PlannerPageViewModel model)
    {
        var ws = workbook.Worksheets.Add("Підсумок");

        ws.Cell("A1").Value = "Meal Planner - підсумок";
        ws.Range("A1:B1").Merge();
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;

        ws.Cell("A3").Value = "Показник";
        ws.Cell("B3").Value = "Значення";

        ws.Range("A3:B3").Style.Font.Bold = true;
        ws.Range("A3:B3").Style.Fill.BackgroundColor = XLColor.LightBlue;

        ws.Cell("A4").Value = "Ціль по білках";
        ws.Cell("B4").Value = model.ProteinTarget;

        ws.Cell("A5").Value = "Ціль по жирах";
        ws.Cell("B5").Value = model.FatTarget;

        ws.Cell("A6").Value = "Ціль по вуглеводах";
        ws.Cell("B6").Value = model.CarbTarget;

        ws.Cell("A7").Value = "Фактично білки";
        ws.Cell("B7").Value = model.ActualProtein;

        ws.Cell("A8").Value = "Фактично жири";
        ws.Cell("B8").Value = model.ActualFat;

        ws.Cell("A9").Value = "Фактично вуглеводи";
        ws.Cell("B9").Value = model.ActualCarb;

        ws.Cell("A10").Value = "Фактичні калорії";
        ws.Cell("B10").Value = model.ActualKcal;

        ws.Cell("A11").Value = "Кількість днів";
        ws.Cell("B11").Value = model.Days;

        ws.Columns().AdjustToContents();
    }

    private void BuildMealsSheet(XLWorkbook workbook, PlannerPageViewModel model)
    {
        var ws = workbook.Worksheets.Add("Меню");

        ws.Cell("A1").Value = "Meal Planner - меню";
        ws.Range("A1:J1").Merge();
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;

        ws.Cell("A3").Value = "Прийом №";
        ws.Cell("B3").Value = "Назва прийому";
        ws.Cell("C3").Value = "Роль";
        ws.Cell("D3").Value = "Продукт";
        ws.Cell("E3").Value = "Кількість";
        ws.Cell("F3").Value = "Одиниця";
        ws.Cell("G3").Value = "Білки";
        ws.Cell("H3").Value = "Жири";
        ws.Cell("I3").Value = "Вуглеводи";
        ws.Cell("J3").Value = "Калорії";

        ws.Range("A3:J3").Style.Font.Bold = true;
        ws.Range("A3:J3").Style.Fill.BackgroundColor = XLColor.LightBlue;

        var row = 4;

        foreach (var meal in model.ResultMeals)
        {
            foreach (var item in meal.Items)
            {
                ws.Cell(row, 1).Value = meal.MealNo;
                ws.Cell(row, 2).Value = meal.MealName;
                ws.Cell(row, 3).Value = item.RoleName;
                ws.Cell(row, 4).Value = item.FoodName;
                ws.Cell(row, 5).Value = item.DisplayQuantityValue;
                ws.Cell(row, 6).Value = item.DisplayUnitName;
                ws.Cell(row, 7).Value = item.Protein;
                ws.Cell(row, 8).Value = item.Fat;
                ws.Cell(row, 9).Value = item.Carb;
                ws.Cell(row, 10).Value = item.Kcal;
                row++;
            }

            ws.Cell(row, 2).Value = "Разом";
            ws.Cell(row, 7).Value = meal.ProteinTotal;
            ws.Cell(row, 8).Value = meal.FatTotal;
            ws.Cell(row, 9).Value = meal.CarbTotal;
            ws.Cell(row, 10).Value = meal.KcalTotal;

            ws.Range(row, 2, row, 10).Style.Font.Bold = true;
            ws.Range(row, 2, row, 10).Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void BuildShoppingListSheet(XLWorkbook workbook, PlannerPageViewModel model)
    {
        var ws = workbook.Worksheets.Add("Список покупок");

        ws.Cell("A1").Value = "Meal Planner - список покупок";
        ws.Range("A1:D1").Merge();
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;

        ws.Cell("A3").Value = "№";
        ws.Cell("B3").Value = "Продукт";
        ws.Cell("C3").Value = "Кількість";
        ws.Cell("D3").Value = "Одиниця";

        ws.Range("A3:D3").Style.Font.Bold = true;
        ws.Range("A3:D3").Style.Fill.BackgroundColor = XLColor.LightBlue;

        var row = 4;
        var index = 1;

        foreach (var item in model.ShoppingItems)
        {
            ws.Cell(row, 1).Value = index;
            ws.Cell(row, 2).Value = item.FoodName;
            ws.Cell(row, 3).Value = decimal.Round(item.DisplayQuantityValue, 0);
            ws.Cell(row, 4).Value = item.DisplayUnitName;

            row++;
            index++;
        }

        ws.Columns().AdjustToContents();
    }

    private List<PlannerImportedExcelItemViewModel> ReadImportedShoppingItems(IXLWorksheet sheet)
    {
        var items = new List<PlannerImportedExcelItemViewModel>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

        for (var row = 4; row <= lastRow; row++)
        {
            var foodName = sheet.Cell(row, 2).GetString().Trim();

            if (string.IsNullOrWhiteSpace(foodName))
            {
                continue;
            }

            var quantityValue = ParseExcelDecimal(sheet.Cell(row, 3));
            var unitName = sheet.Cell(row, 4).GetString().Trim();

            items.Add(new PlannerImportedExcelItemViewModel
            {
                RowNo = items.Count + 1,
                FoodName = foodName,
                QuantityValue = quantityValue,
                UnitName = unitName
            });
        }

        return items;
    }

    private decimal ParseExcelDecimal(IXLCell cell)
    {
        if (cell.TryGetValue<decimal>(out var decimalValue))
        {
            return decimalValue;
        }

        var raw = cell.GetString().Trim();

        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariantValue))
        {
            return invariantValue;
        }

        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("uk-UA"), out var ukValue))
        {
            return ukValue;
        }

        return 0;
    }

    private List<decimal> GetMacroWeights(int mealsPerDay)
    {
        return mealsPerDay switch
        {
            2 => new List<decimal> { 0.45m, 0.55m },
            3 => new List<decimal> { 0.30m, 0.35m, 0.35m },
            4 => new List<decimal> { 0.25m, 0.25m, 0.25m, 0.25m },
            _ => Enumerable.Repeat(1m / mealsPerDay, mealsPerDay).ToList()
        };
    }

    private List<decimal> DistributeMacro(
        List<PlannerMealInputViewModel> meals,
        decimal total,
        Func<PlannerMealInputViewModel, bool> hasMacro)
    {
        var weights = GetMacroWeights(meals.Count);

        var indexes = meals
            .Select((meal, i) => new { meal, i })
            .Where(x => hasMacro(x.meal))
            .Select(x => x.i)
            .ToList();

        var result = Enumerable.Repeat(0m, meals.Count).ToList();

        if (indexes.Count == 0 || total <= 0)
        {
            return result;
        }

        var totalWeight = indexes.Sum(i => weights[i]);

        foreach (var i in indexes)
        {
            result[i] = decimal.Round(total * weights[i] / totalWeight, 1);
        }

        return result;
    }

    private async Task FillOptionsAsync(PlannerPageViewModel model)
    {
        var foods = await _context.Foods
            .AsNoTracking()
            .Include(x => x.Icon)
            .OrderBy(x => x.Name)
            .ToListAsync();

        model.ProteinFoods = foods
            .Where(x => x.Category == FoodCategory.Protein)
            .Select(ToPickerFood)
            .ToList();

        model.CarbFoods = foods
            .Where(x => x.Category == FoodCategory.Carb)
            .Select(ToPickerFood)
            .ToList();

        model.FatFoods = foods
            .Where(x => x.Category == FoodCategory.Fat)
            .Select(ToPickerFood)
            .ToList();
    }

    private PlannerPickerFoodViewModel ToPickerFood(Food food)
    {
        return new PlannerPickerFoodViewModel
        {
            Id = food.Id,
            Name = food.Name,
            Emoji = food.Icon?.Emoji ?? "🍽️",
            ProteinPer100 = food.ProteinPer100,
            CarbPer100 = food.CarbsPer100,
            FatPer100 = food.FatPer100
        };
    }

    private async Task SetCurrentUserAsync(PlannerPageViewModel model)
    {
        if (model.UserId.HasValue)
        {
            return;
        }

        model.UserId = await _context.Users
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }

    private void NormalizeMeals(PlannerPageViewModel model)
    {
        model.MealsPerDay = Math.Clamp(model.MealsPerDay, 2, 4);
        model.Days = Math.Clamp(model.Days, 1, 7);

        model.Meals ??= new List<PlannerMealInputViewModel>();

        while (model.Meals.Count < 4)
        {
            model.Meals.Add(new PlannerMealInputViewModel());
        }

        for (var i = 0; i < 4; i++)
        {
            model.Meals[i].MealNo = i + 1;

            if (string.IsNullOrWhiteSpace(model.Meals[i].MealName))
            {
                model.Meals[i].MealName = $"Прийом {i + 1}";
            }
        }
    }

    private List<PlannerMealInputViewModel> GetActiveMeals(PlannerPageViewModel model)
    {
        return model.Meals
            .OrderBy(x => x.MealNo)
            .Take(model.MealsPerDay)
            .ToList();
    }

    private List<decimal> GetCarbDistributionWeights(int mealsPerDay)
    {
        return mealsPerDay switch
        {
            2 => new List<decimal> { 0.45m, 0.55m },
            3 => new List<decimal> { 0.30m, 0.35m, 0.35m },
            4 => new List<decimal> { 0.25m, 0.25m, 0.25m, 0.25m },
            _ => Enumerable.Repeat(1m / mealsPerDay, mealsPerDay).ToList()
        };
    }

    private List<decimal> BuildCarbTargetsForMeals(List<PlannerMealInputViewModel> activeMeals, decimal remainingCarb)
    {
        var weights = GetCarbDistributionWeights(activeMeals.Count);

        var carbMealIndexes = activeMeals
            .Select((meal, index) => new { meal, index })
            .Where(x => x.meal.CarbFoodId.HasValue)
            .Select(x => x.index)
            .ToList();

        var targets = Enumerable.Repeat(0m, activeMeals.Count).ToList();

        if (remainingCarb <= 0 || carbMealIndexes.Count == 0)
        {
            return targets;
        }

        var totalWeight = carbMealIndexes.Sum(index => weights[index]);

        if (totalWeight <= 0)
        {
            var equalPart = remainingCarb / carbMealIndexes.Count;

            foreach (var index in carbMealIndexes)
            {
                targets[index] = equalPart;
            }

            return targets;
        }

        foreach (var index in carbMealIndexes)
        {
            targets[index] = decimal.Round(remainingCarb * weights[index] / totalWeight, 1);
        }

        return targets;
    }

    private async Task ApplyUserDefaultsAsync(PlannerPageViewModel model)
    {
        if (!model.UserId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Не знайдено користувача для роботи конструктора.");
            return;
        }

        var configuration = await _context.Configurations
            .AsNoTracking()
            .Include(x => x.ActiveMacro)
            .FirstOrDefaultAsync(x => x.UserId == model.UserId.Value);

        if (configuration == null)
        {
            ModelState.AddModelError(string.Empty, "Для поточного користувача не знайдено конфігурацію.");
            return;
        }

        if (configuration.MealsPerDay > 0)
        {
            model.MealsPerDay = Math.Clamp(configuration.MealsPerDay, 2, 4);
        }

        if (configuration.ActiveMacro != null)
        {
            model.ProteinTarget = configuration.ActiveMacro.ProteinG;
            model.CarbTarget = configuration.ActiveMacro.CarbsG;
            model.FatTarget = configuration.ActiveMacro.FatG;
        }

        NormalizeMeals(model);
    }

    private void ValidatePlannerInput(PlannerPageViewModel model)
    {
        if (!model.UserId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Не знайдено користувача для збереження плану.");
        }

        if (model.ProteinTarget <= 0)
        {
            ModelState.AddModelError("ProteinTarget", "Потрібно вказати ціль по білках.");
        }

        if (model.CarbTarget <= 0)
        {
            ModelState.AddModelError("CarbTarget", "Потрібно вказати ціль по вуглеводах.");
        }

        if (model.FatTarget <= 0)
        {
            ModelState.AddModelError("FatTarget", "Потрібно вказати ціль по жирах.");
        }

        foreach (var meal in GetActiveMeals(model))
        {
            if (!meal.ProteinFoodId.HasValue && !meal.CarbFoodId.HasValue && !meal.FatFoodId.HasValue)
            {
                ModelState.AddModelError($"Meals[{meal.MealNo - 1}].MealName", $"Для прийому {meal.MealNo} потрібно вибрати хоча б один продукт.");
            }
        }
    }

    private async Task BuildResultAsync(PlannerPageViewModel model)
    {
        var activeMeals = GetActiveMeals(model);

        var allIds = activeMeals
            .SelectMany(x => new[] { x.ProteinFoodId, x.CarbFoodId, x.FatFoodId })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var foods = await _context.Foods
            .AsNoTracking()
            .Include(x => x.Icon)
            .Include(x => x.Per100Unit)
            .Where(x => allIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        model.ResultMeals.Clear();
        model.ShoppingItems.Clear();

        model.ActualProtein = 0;
        model.ActualCarb = 0;
        model.ActualFat = 0;
        model.ActualKcal = 0;

        var proteinSlotData = new List<ProteinSlotData>();
        var fixedProtein = 0m;
        var fixedFatFromProtein = 0m;
        var fixedCarbFromProtein = 0m;

        foreach (var meal in activeMeals)
        {
            var proteinFood = GetFood(foods, meal.ProteinFoodId);

            if (proteinFood == null || IsEmptyFood(proteinFood))
            {
                proteinSlotData.Add(ProteinSlotData.Empty());
                continue;
            }

            if (IsEggOrOmelette(proteinFood))
            {
                var weight = 165m;
                proteinSlotData.Add(ProteinSlotData.Fixed(weight, 3m, "шт"));
                fixedProtein += CalcMacro(weight, proteinFood.ProteinPer100);
                fixedFatFromProtein += CalcMacro(weight, proteinFood.FatPer100);
                fixedCarbFromProtein += CalcMacro(weight, proteinFood.CarbsPer100);
                continue;
            }

            if (IsCottageCheese(proteinFood))
            {
                var weight = 200m;
                proteinSlotData.Add(ProteinSlotData.Fixed(weight, 200m, "gram"));
                fixedProtein += CalcMacro(weight, proteinFood.ProteinPer100);
                fixedFatFromProtein += CalcMacro(weight, proteinFood.FatPer100);
                fixedCarbFromProtein += CalcMacro(weight, proteinFood.CarbsPer100);
                continue;
            }

            if (IsProteinPowder(proteinFood))
            {
                var weight = 30m;
                proteinSlotData.Add(ProteinSlotData.Fixed(weight, 1m, "скуп"));
                fixedProtein += CalcMacro(weight, proteinFood.ProteinPer100);
                fixedFatFromProtein += CalcMacro(weight, proteinFood.FatPer100);
                fixedCarbFromProtein += CalcMacro(weight, proteinFood.CarbsPer100);
                continue;
            }

            proteinSlotData.Add(ProteinSlotData.Dynamic());
        }

        var remainingProtein = model.ProteinTarget - fixedProtein;
        if (remainingProtein < 0)
        {
            remainingProtein = 0;
        }

        var proteinTargets = DistributeMacro(
            activeMeals,
            remainingProtein,
            m => m.ProteinFoodId.HasValue);

        var remainingCarb = model.CarbTarget - fixedCarbFromProtein;
        if (remainingCarb < 0)
        {
            remainingCarb = 0;
        }

        var carbTargets = DistributeMacro(
            activeMeals,
            remainingCarb,
            m => m.CarbFoodId.HasValue);

        var mealsTemp = new List<MealTempData>();
        var usedProtein = fixedProtein;
        var usedCarb = fixedCarbFromProtein;
        var usedFat = fixedFatFromProtein;

        for (var index = 0; index < activeMeals.Count; index++)
        {
            var mealInput = activeMeals[index];

            var proteinFood = GetFood(foods, mealInput.ProteinFoodId);
            var carbFood = GetFood(foods, mealInput.CarbFoodId);
            var fatFood = GetFood(foods, mealInput.FatFoodId);

            var proteinItem = BuildProteinItem(proteinFood, proteinSlotData[index], proteinTargets[index]);
            var carbItem = BuildCarbItem(carbFood, carbTargets[index]);

            if (proteinItem != null)
            {
                usedProtein += proteinItem.Protein;
                usedFat += proteinItem.Fat;
                usedCarb += proteinItem.Carb;
            }

            if (carbItem != null)
            {
                usedProtein += carbItem.Protein;
                usedFat += carbItem.Fat;
                usedCarb += carbItem.Carb;
            }

            mealsTemp.Add(new MealTempData
            {
                MealNo = mealInput.MealNo,
                MealName = mealInput.MealName,
                ProteinItem = proteinItem,
                CarbItem = carbItem,
                FatFood = fatFood
            });
        }

        var remainingFat = model.FatTarget - usedFat;
        var fatOverkill = remainingFat < -5m;

        if (remainingFat < 0)
        {
            remainingFat = 0;
        }

        var fatTargets = DistributeMacro(
            activeMeals,
            remainingFat,
            m => m.FatFoodId.HasValue);

        for (var index = 0; index < mealsTemp.Count; index++)
        {
            var temp = mealsTemp[index];

            var mealResult = new PlannerMealResultViewModel
            {
                MealNo = temp.MealNo,
                MealName = temp.MealName
            };

            AddItemIfExists(mealResult, temp.ProteinItem);
            AddItemIfExists(mealResult, temp.CarbItem);

            var fatItem = BuildFatItem(temp.FatFood, fatTargets[index], fatOverkill);
            AddItemIfExists(mealResult, fatItem);

            mealResult.ProteinTotal = decimal.Round(mealResult.Items.Sum(x => x.Protein), 1);
            mealResult.CarbTotal = decimal.Round(mealResult.Items.Sum(x => x.Carb), 1);
            mealResult.FatTotal = decimal.Round(mealResult.Items.Sum(x => x.Fat), 1);
            mealResult.KcalTotal = decimal.Round(mealResult.Items.Sum(x => x.Kcal), 0);

            model.ActualProtein += mealResult.ProteinTotal;
            model.ActualCarb += mealResult.CarbTotal;
            model.ActualFat += mealResult.FatTotal;
            model.ActualKcal += mealResult.KcalTotal;

            model.ResultMeals.Add(mealResult);
        }

        AdjustResultMacros(model, mealsTemp, proteinSlotData, foods, fatOverkill);

        BuildShoppingList(model);

        model.ActualProtein = decimal.Round(model.ActualProtein, 1);
        model.ActualCarb = decimal.Round(model.ActualCarb, 1);
        model.ActualFat = decimal.Round(model.ActualFat, 1);
        model.ActualKcal = decimal.Round(model.ActualKcal, 0);
        model.ExportText = BuildExportText(model);
        model.HasResult = true;
    }

    private void AdjustResultMacros(
        PlannerPageViewModel model,
        List<MealTempData> mealsTemp,
        List<ProteinSlotData> proteinSlotData,
        Dictionary<Guid, Food> foods,
        bool fatOverkill)
    {
        AdjustProteinResult(model, mealsTemp, proteinSlotData, foods);
        RecalculateResultTotals(model);

        AdjustCarbResult(model, mealsTemp, foods);
        RecalculateResultTotals(model);

        if (!fatOverkill)
        {
            AdjustFatResult(model, mealsTemp, foods);
            RecalculateResultTotals(model);
        }
    }

    private void AdjustProteinResult(
        PlannerPageViewModel model,
        List<MealTempData> mealsTemp,
        List<ProteinSlotData> proteinSlotData,
        Dictionary<Guid, Food> foods)
    {
        var proteinDiff = model.ProteinTarget - model.ActualProtein;

        if (decimal.Abs(proteinDiff) < 0.5m)
        {
            return;
        }

        for (var index = model.ResultMeals.Count - 1; index >= 0; index--)
        {
            if (proteinSlotData[index].Type != ProteinSlotType.Dynamic)
            {
                continue;
            }

            var item = model.ResultMeals[index].Items
                .LastOrDefault(x => x.RoleName == "Білковий продукт" && x.FoodId.HasValue);

            if (item == null)
            {
                continue;
            }

            var food = GetFood(foods, item.FoodId);

            if (food == null || food.ProteinPer100 <= 0)
            {
                continue;
            }

            ApplyMacroDiffToItem(item, food, proteinDiff, food.ProteinPer100);
            return;
        }
    }

    private void AdjustCarbResult(
        PlannerPageViewModel model,
        List<MealTempData> mealsTemp,
        Dictionary<Guid, Food> foods)
    {
        var carbDiff = model.CarbTarget - model.ActualCarb;

        if (decimal.Abs(carbDiff) < 0.5m)
        {
            return;
        }

        for (var index = model.ResultMeals.Count - 1; index >= 0; index--)
        {
            if (mealsTemp[index].CarbItem == null)
            {
                continue;
            }

            var item = model.ResultMeals[index].Items
                .LastOrDefault(x => x.RoleName == "Вуглеводний продукт" && x.FoodId.HasValue);

            if (item == null)
            {
                continue;
            }

            var food = GetFood(foods, item.FoodId);

            if (food == null || food.CarbsPer100 <= 0)
            {
                continue;
            }

            ApplyMacroDiffToItem(item, food, carbDiff, food.CarbsPer100);
            return;
        }
    }

    private void AdjustFatResult(
        PlannerPageViewModel model,
        List<MealTempData> mealsTemp,
        Dictionary<Guid, Food> foods)
    {
        var fatDiff = model.FatTarget - model.ActualFat;

        if (decimal.Abs(fatDiff) < 0.5m)
        {
            return;
        }

        for (var index = model.ResultMeals.Count - 1; index >= 0; index--)
        {
            if (mealsTemp[index].FatFood == null || IsEmptyFood(mealsTemp[index].FatFood!))
            {
                continue;
            }

            var item = model.ResultMeals[index].Items
                .LastOrDefault(x => x.RoleName == "Жировий продукт" && x.FoodId.HasValue);

            if (item == null)
            {
                continue;
            }

            var food = GetFood(foods, item.FoodId);

            if (food == null || food.FatPer100 <= 0)
            {
                continue;
            }

            ApplyMacroDiffToItem(item, food, fatDiff, food.FatPer100);
            return;
        }
    }

    private void ApplyMacroDiffToItem(
        PlannerMealResultItemViewModel item,
        Food food,
        decimal diff,
        decimal macroPer100)
    {
        if (macroPer100 <= 0)
        {
            return;
        }

        var weightDelta = decimal.Round(diff / macroPer100 * 100m, 0, MidpointRounding.AwayFromZero);
        var newWeight = item.QuantityValue + weightDelta;

        if (newWeight < 0)
        {
            newWeight = 0;
        }

        item.QuantityValue = newWeight;
        item.DisplayQuantityValue = newWeight;
        item.DisplayUnitName = food.Per100Unit.Name;
        item.UnitName = food.Per100Unit.Name;
        item.Protein = CalcMacro(newWeight, food.ProteinPer100);
        item.Carb = CalcMacro(newWeight, food.CarbsPer100);
        item.Fat = CalcMacro(newWeight, food.FatPer100);
        item.Kcal = CalcMacro(newWeight, food.KcalPer100);
    }

    private void RecalculateResultTotals(PlannerPageViewModel model)
    {
        model.ActualProtein = 0;
        model.ActualCarb = 0;
        model.ActualFat = 0;
        model.ActualKcal = 0;

        foreach (var meal in model.ResultMeals)
        {
            meal.ProteinTotal = decimal.Round(meal.Items.Sum(x => x.Protein), 1);
            meal.CarbTotal = decimal.Round(meal.Items.Sum(x => x.Carb), 1);
            meal.FatTotal = decimal.Round(meal.Items.Sum(x => x.Fat), 1);
            meal.KcalTotal = decimal.Round(meal.Items.Sum(x => x.Kcal), 0);

            model.ActualProtein += meal.ProteinTotal;
            model.ActualCarb += meal.CarbTotal;
            model.ActualFat += meal.FatTotal;
            model.ActualKcal += meal.KcalTotal;
        }

        model.ActualProtein = decimal.Round(model.ActualProtein, 1);
        model.ActualCarb = decimal.Round(model.ActualCarb, 1);
        model.ActualFat = decimal.Round(model.ActualFat, 1);
        model.ActualKcal = decimal.Round(model.ActualKcal, 0);
    }

    private void BuildShoppingList(PlannerPageViewModel model)
    {
        var shoppingMap = new Dictionary<Guid, PlannerShoppingItemViewModel>();

        foreach (var meal in model.ResultMeals)
        {
            foreach (var item in meal.Items)
            {
                if (!item.FoodId.HasValue || item.QuantityValue <= 0)
                {
                    continue;
                }

                if (!shoppingMap.ContainsKey(item.FoodId.Value))
                {
                    shoppingMap[item.FoodId.Value] = new PlannerShoppingItemViewModel
                    {
                        FoodId = item.FoodId.Value,
                        QuantityUnitId = item.QuantityUnitId,
                        FoodName = item.FoodName,
                        Emoji = item.Emoji,
                        UnitName = item.UnitName,
                        DisplayUnitName = item.DisplayUnitName,
                        TotalQuantity = 0,
                        DisplayQuantityValue = 0
                    };
                }

                shoppingMap[item.FoodId.Value].TotalQuantity += item.QuantityValue * model.Days;
                shoppingMap[item.FoodId.Value].DisplayQuantityValue += item.DisplayQuantityValue * model.Days;
            }
        }

        model.ShoppingItems = shoppingMap.Values
            .OrderBy(x => x.FoodName)
            .ToList();
    }

    private string BuildExportText(PlannerPageViewModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("MEAL PLANNER");
        sb.AppendLine();
        sb.AppendLine($"Білки: {model.ActualProtein} / {model.ProteinTarget}");
        sb.AppendLine($"Жири: {model.ActualFat} / {model.FatTarget}");
        sb.AppendLine($"Вуглеводи: {model.ActualCarb} / {model.CarbTarget}");
        sb.AppendLine($"Калорії: {model.ActualKcal}");
        sb.AppendLine();

        foreach (var meal in model.ResultMeals)
        {
            sb.AppendLine($"{meal.MealName}:");

            foreach (var item in meal.Items)
            {
                sb.AppendLine($"- {item.FoodName}: {item.DisplayQuantityValue} {item.DisplayUnitName}");
            }

            sb.AppendLine($"  Разом: Б {meal.ProteinTotal} / Ж {meal.FatTotal} / В {meal.CarbTotal} / {meal.KcalTotal} ккал");
            sb.AppendLine();
        }

        sb.AppendLine($"Список покупок на {model.Days} дн.:");

        foreach (var item in model.ShoppingItems)
        {
            sb.AppendLine($"- {item.FoodName}: {decimal.Round(item.DisplayQuantityValue, 0)} {item.DisplayUnitName}");
        }

        return sb.ToString();
    }

    private async Task<(Guid planId, Guid shopListId)> SavePlanAsync(PlannerPageViewModel model)
    {
        var configuration = await _context.Configurations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == model.UserId);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            UserId = model.UserId!.Value,
            MacroId = configuration?.ActiveMacroId,
            Days = model.Days,
            Status = "generated",
            CreatedAt = DateTime.UtcNow
        };

        _context.Plans.Add(plan);

        foreach (var day in Enumerable.Range(1, model.Days))
        {
            foreach (var mealResult in model.ResultMeals)
            {
                var meal = new Meal
                {
                    Id = Guid.NewGuid(),
                    PlanId = plan.Id,
                    DayNo = day,
                    MealNo = mealResult.MealNo,
                    Name = mealResult.MealName
                };

                _context.Meals.Add(meal);

                foreach (var item in mealResult.Items.Where(x => x.FoodId.HasValue && x.QuantityValue > 0))
                {
                    var mealItem = new MealItem
                    {
                        Id = Guid.NewGuid(),
                        MealId = meal.Id,
                        FoodId = item.FoodId!.Value,
                        QuantityValue = item.QuantityValue,
                        QuantityUnitId = item.QuantityUnitId,
                        Per100UnitId = item.QuantityUnitId,
                        ProteinPer100 = item.QuantityValue > 0 ? decimal.Round(item.Protein / item.QuantityValue * 100m, 2) : 0,
                        CarbsPer100 = item.QuantityValue > 0 ? decimal.Round(item.Carb / item.QuantityValue * 100m, 2) : 0,
                        FatPer100 = item.QuantityValue > 0 ? decimal.Round(item.Fat / item.QuantityValue * 100m, 2) : 0,
                        KcalPer100 = item.QuantityValue > 0 ? decimal.Round(item.Kcal / item.QuantityValue * 100m, 2) : 0,
                        IsLocked = false
                    };

                    _context.MealItems.Add(mealItem);
                }
            }
        }

        var shopList = new ShopList
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Days = model.Days,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShopLists.Add(shopList);

        foreach (var shoppingItem in model.ShoppingItems.Where(x => x.TotalQuantity > 0))
        {
            var entity = new ShopItem
            {
                Id = Guid.NewGuid(),
                ListId = shopList.Id,
                FoodId = shoppingItem.FoodId,
                TotalQuantityValue = decimal.Round(shoppingItem.TotalQuantity, 0),
                QuantityUnitId = shoppingItem.QuantityUnitId
            };

            _context.ShopItems.Add(entity);
        }

        await _context.SaveChangesAsync();

        return (plan.Id, shopList.Id);
    }

    private Food? GetFood(Dictionary<Guid, Food> foods, Guid? foodId)
    {
        if (!foodId.HasValue)
        {
            return null;
        }

        return foods.TryGetValue(foodId.Value, out var food) ? food : null;
    }

    private bool IsEmptyFood(Food food)
    {
        var name = food.Name.Trim().ToLower();
        return name.Contains("без м’яса") || name.Contains("без масла") || name.Contains("без мяса");
    }

    private bool IsEggOrOmelette(Food food)
    {
        var name = food.Name.Trim().ToLower();
        return name.Contains("яйця") || name.Contains("омлет") || name.Contains("яйца");
    }

    private bool IsCottageCheese(Food food)
    {
        var name = food.Name.Trim().ToLower();
        return name.Contains("сир кисломолочний") || name.Contains("творог");
    }

    private bool IsProteinPowder(Food food)
    {
        var name = food.Name.Trim().ToLower();
        return name.Contains("протеїн") || name.Contains("протеин") || name.Contains("whey");
    }

    private decimal CalcMacro(decimal weight, decimal macroPer100)
    {
        return decimal.Round((weight / 100m) * macroPer100, 1);
    }

    private PlannerMealResultItemViewModel? BuildProteinItem(Food? food, ProteinSlotData slotData, decimal targetProtein)
    {
        if (food == null || slotData.Type == ProteinSlotType.Empty)
        {
            return null;
        }

        decimal weight;
        decimal displayQuantity;
        string displayUnit;

        if (slotData.Type == ProteinSlotType.Fixed)
        {
            weight = slotData.WeightGrams;
            displayQuantity = slotData.DisplayQuantity;
            displayUnit = slotData.DisplayUnit;
        }
        else
        {
            if (food.ProteinPer100 <= 0)
            {
                return new PlannerMealResultItemViewModel
                {
                    FoodId = food.Id,
                    QuantityUnitId = food.Per100UnitId,
                    RoleName = "Білковий продукт",
                    FoodName = food.Name,
                    Emoji = food.Icon?.Emoji ?? "🍽️",
                    UnitName = food.Per100Unit.Name,
                    DisplayUnitName = food.Per100Unit.Name,
                    QuantityValue = 0,
                    DisplayQuantityValue = 0,
                    Protein = 0,
                    Carb = 0,
                    Fat = 0,
                    Kcal = 0,
                    Note = "У продукті немає достатньо білка для автоматичного розрахунку."
                };
            }

            weight = decimal.Round(targetProtein / food.ProteinPer100 * 100m, 0, MidpointRounding.AwayFromZero);
            displayQuantity = weight;
            displayUnit = food.Per100Unit.Name;
        }

        return BuildItemFromWeight(food, "Білковий продукт", weight, displayQuantity, displayUnit, string.Empty);
    }

    private PlannerMealResultItemViewModel? BuildCarbItem(Food? food, decimal targetCarb)
    {
        if (food == null || IsEmptyFood(food))
        {
            return null;
        }

        if (food.CarbsPer100 <= 0)
        {
            return new PlannerMealResultItemViewModel
            {
                FoodId = food.Id,
                QuantityUnitId = food.Per100UnitId,
                RoleName = "Вуглеводний продукт",
                FoodName = food.Name,
                Emoji = food.Icon?.Emoji ?? "🍽️",
                UnitName = food.Per100Unit.Name,
                DisplayUnitName = food.Per100Unit.Name,
                QuantityValue = 0,
                DisplayQuantityValue = 0,
                Protein = 0,
                Carb = 0,
                Fat = 0,
                Kcal = 0,
                Note = "У продукті немає достатньо вуглеводів для автоматичного розрахунку."
            };
        }

        var weight = decimal.Round(targetCarb / food.CarbsPer100 * 100m, 0, MidpointRounding.AwayFromZero);

        return BuildItemFromWeight(
            food,
            "Вуглеводний продукт",
            weight,
            weight,
            food.Per100Unit.Name,
            string.Empty);
    }

    private PlannerMealResultItemViewModel? BuildFatItem(Food? food, decimal targetFat, bool fatOverkill)
    {
        if (food == null || IsEmptyFood(food))
        {
            return null;
        }

        if (fatOverkill)
        {
            return BuildItemFromWeight(
                food,
                "Жировий продукт",
                0,
                0,
                food.Per100Unit.Name,
                "Додатковий жир не потрібен: ціль уже перекрита іншими продуктами.");
        }

        if (food.FatPer100 <= 0)
        {
            return BuildItemFromWeight(
                food,
                "Жировий продукт",
                0,
                0,
                food.Per100Unit.Name,
                "У продукті немає достатньо жирів для автоматичного розрахунку.");
        }

        var weight = decimal.Round(targetFat / food.FatPer100 * 100m, 0, MidpointRounding.AwayFromZero);

        return BuildItemFromWeight(
            food,
            "Жировий продукт",
            weight,
            weight,
            food.Per100Unit.Name,
            string.Empty);
    }

    private PlannerMealResultItemViewModel BuildItemFromWeight(
        Food food,
        string roleName,
        decimal weight,
        decimal displayQuantity,
        string displayUnitName,
        string note)
    {
        var protein = CalcMacro(weight, food.ProteinPer100);
        var carb = CalcMacro(weight, food.CarbsPer100);
        var fat = CalcMacro(weight, food.FatPer100);
        var kcal = CalcMacro(weight, food.KcalPer100);

        return new PlannerMealResultItemViewModel
        {
            FoodId = food.Id,
            QuantityUnitId = food.Per100UnitId,
            RoleName = roleName,
            FoodName = food.Name,
            Emoji = food.Icon?.Emoji ?? "🍽️",
            UnitName = food.Per100Unit.Name,
            DisplayUnitName = string.IsNullOrWhiteSpace(displayUnitName)
                ? food.Per100Unit.Name
                : displayUnitName,
            QuantityValue = weight,
            DisplayQuantityValue = displayQuantity,
            Protein = protein,
            Carb = carb,
            Fat = fat,
            Kcal = kcal,
            Note = note
        };
    }

    private void AddItemIfExists(PlannerMealResultViewModel mealResult, PlannerMealResultItemViewModel? item)
    {
        if (item != null)
        {
            mealResult.Items.Add(item);
        }
    }

    private sealed class MealTempData
    {
        public int MealNo { get; set; }
        public string MealName { get; set; } = string.Empty;
        public PlannerMealResultItemViewModel? ProteinItem { get; set; }
        public PlannerMealResultItemViewModel? CarbItem { get; set; }
        public Food? FatFood { get; set; }
    }

    private sealed class ProteinSlotData
    {
        public ProteinSlotType Type { get; private set; }
        public decimal WeightGrams { get; private set; }
        public decimal DisplayQuantity { get; private set; }
        public string DisplayUnit { get; private set; } = "gram";

        public static ProteinSlotData Empty() => new() { Type = ProteinSlotType.Empty };

        public static ProteinSlotData Dynamic() => new() { Type = ProteinSlotType.Dynamic };

        public static ProteinSlotData Fixed(decimal weightGrams, decimal displayQuantity, string displayUnit)
            => new()
            {
                Type = ProteinSlotType.Fixed,
                WeightGrams = weightGrams,
                DisplayQuantity = displayQuantity,
                DisplayUnit = displayUnit
            };
    }

    private enum ProteinSlotType
    {
        Empty = 0,
        Dynamic = 1,
        Fixed = 2
    }
}