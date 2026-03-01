namespace MealPlanner.Domain.Entities;

public class MacroLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? MacroId { get; set; }
    public string Event { get; set; } = null!;
    public int ProteinG { get; set; }
    public int CarbsG { get; set; }
    public int FatG { get; set; }
    public DateTime ChangedAt { get; set; }

    public User User { get; set; } = null!;
    public Macro? Macro { get; set; }
}