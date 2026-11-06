using Telegram.Bot;

namespace YensTelegramLuckyDraw.Services;

public interface IStandardTelegramBotClient
{
    ITelegramBotClient Client { get; }
}

public interface ILateralThinkingTelegramBotClient
{
    ITelegramBotClient Client { get; }
}

public sealed record StandardTelegramBotClient(ITelegramBotClient Client) : IStandardTelegramBotClient;

public sealed record LateralThinkingTelegramBotClient(ITelegramBotClient Client) : ILateralThinkingTelegramBotClient;

public static class TelegramBotClientFactory
{
    public static ITelegramBotClient Create(string? token) =>
        new TelegramBotClient(string.IsNullOrWhiteSpace(token) ? string.Empty : token);
}
