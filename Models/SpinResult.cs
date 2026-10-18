using System.ComponentModel.DataAnnotations;

namespace YensTelegramLuckyDraw.Models;

public class SpinResult
{
    [Key]
    public int Id { get; set; }
    public string Reward { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    // Optional link to DrawState (the single Yen draw state)
    public int? DrawStateId { get; set; }
    public DrawState? DrawState { get; set; }
    public bool AdminAdded { get; set; }
}
