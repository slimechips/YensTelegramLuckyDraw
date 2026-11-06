namespace YensTelegramLuckyDraw.Models;

public sealed class LateralPuzzleParticipant
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public long TelegramUserId { get; set; }
    public LateralPuzzleSession Session { get; set; } = null!;
}
