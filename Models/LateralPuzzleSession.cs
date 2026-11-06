namespace YensTelegramLuckyDraw.Models;

public sealed class LateralPuzzleSession
{
    public int Id { get; set; }
    public long ChatId { get; set; }
    public int PuzzleNumber { get; set; }
    public string PuzzleText { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartedAt { get; set; }
    public ICollection<LateralPuzzleMessage> Messages { get; set; } = new List<LateralPuzzleMessage>();
    public ICollection<LateralPuzzleParticipant> Participants { get; set; } = new List<LateralPuzzleParticipant>();
}
