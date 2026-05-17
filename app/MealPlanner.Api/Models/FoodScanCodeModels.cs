namespace MealPlanner.Api.Models;

public class CreateFoodScanCodeRequest
{
    public Guid FoodId { get; set; }
    public Guid UserId { get; set; }
    public string CodeValue { get; set; } = "";
    public string CodeType { get; set; } = "Barcode";
    public string? Note { get; set; }
}

public class ScanFoodCodeRequest
{
    public string CodeValue { get; set; } = "";
    public Guid? UserId { get; set; }
    public string Source { get; set; } = "Web camera";
}

public class FoodScanCodeResponse
{
    public Guid Id { get; set; }
    public Guid FoodId { get; set; }
    public string FoodName { get; set; } = "";
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public string CodeValue { get; set; } = "";
    public string CodeType { get; set; } = "";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ScanFoodCodeResponse
{
    public bool Found { get; set; }
    public string Message { get; set; } = "";
    public Guid? FoodId { get; set; }
    public string? FoodName { get; set; }
    public string? Category { get; set; }
    public decimal? ProteinPer100 { get; set; }
    public decimal? CarbsPer100 { get; set; }
    public decimal? FatPer100 { get; set; }
    public decimal? KcalPer100 { get; set; }
}

public class FoodScanLogResponse
{
    public Guid Id { get; set; }
    public string ScannedCode { get; set; } = "";
    public string Result { get; set; } = "";
    public string Source { get; set; } = "";
    public string? FoodName { get; set; }
    public string? UserName { get; set; }
    public DateTime ScannedAt { get; set; }
}
