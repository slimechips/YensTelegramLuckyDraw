namespace YensTelegramLuckyDraw.Models.Settings;

public record OpenAiSettings
{
  public string ModelName { get; set; } = "gpt-6.1-sol";
  public string ApiKey { get; set; } = string.Empty;
}
