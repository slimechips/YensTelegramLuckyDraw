namespace YensTelegramLuckyDraw.Models;

public sealed class LateralPuzzleMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int Sequence { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public LateralPuzzleSession Session { get; set; } = null!;
}
