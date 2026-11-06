namespace YensTelegramLuckyDraw.Models.Settings;

public record LateralThinkingSettings
{
  public string FileName { get; set; } = "lateral_thinking_puzzles_advanced.md";
  public long AdminUserId { get; set; }
}
