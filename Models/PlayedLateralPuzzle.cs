namespace YensTelegramLuckyDraw.Models;

public sealed class PlayedLateralPuzzle
{
    public int Id { get; set; }
    public int PuzzleNumber { get; set; }
    public long TelegramUserId { get; set; }
    public long ChatId { get; set; }
    public DateTime PlayedAt { get; set; }
}
