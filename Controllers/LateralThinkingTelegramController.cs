using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using YensTelegramLuckyDraw.Models.Settings;
using YensTelegramLuckyDraw.Services;

namespace YensTelegramLuckyDraw.Controllers;

[ApiController]
[Route("api/lateral-thinking/telegram")]
public sealed class LateralThinkingTelegramController(
    ILateralThinkingTelegramBotClient botClient,
    LateralPuzzleService lateralPuzzleService,
    IOptions<LateralThinkingSettings> settings) : ControllerBase
{
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> ChatLocks = new();

    [HttpPost("update")]
    public async Task<IActionResult> ReceiveUpdate([FromBody] Update update)
    {
        var message = update.Message;
        var text = message?.Text?.Trim();
        var user = message?.From;
        var chat = message?.Chat;

        if (string.IsNullOrWhiteSpace(text) || user is null || chat is null)
        {
            return Ok();
        }

        var command = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .Split('@', 2)[0];

        if (text.StartsWith("/", StringComparison.Ordinal))
        {
            if (string.Equals(command, "/newpuzzle", StringComparison.OrdinalIgnoreCase))
            {
                await SendPuzzleResponseAsync(chat.Id,
                    token => lateralPuzzleService.StartPuzzleAsync(chat.Id, user.Id, token));
            }
            else if (string.Equals(command, "/resetpuzzle", StringComparison.OrdinalIgnoreCase))
            {
                await SendPuzzleResponseAsync(chat.Id,
                    token => lateralPuzzleService.ResetPuzzleAsync(chat.Id, user.Id, token));
            }
            else if (string.Equals(command, "/adminclearpuzzles", StringComparison.OrdinalIgnoreCase))
            {
                if (settings.Value.AdminUserId == 0)
                {
                    await botClient.Client.SendMessage(chat.Id,
                        "Puzzle admin is not configured. Set LateralThinkingSettings:AdminUserId first.");
                }
                else if (user.Id != settings.Value.AdminUserId)
                {
                    await botClient.Client.SendMessage(chat.Id, "Only the configured puzzle admin can clear puzzle data.");
                }
                else
                {
                    await SendPuzzleResponseAsync(chat.Id,
                        token => lateralPuzzleService.ClearAllSessionDataAsync(token));
                }
            }
            else
            {
                await botClient.Client.SendMessage(chat.Id,
                    "Commands:\n/newpuzzle - start a random lateral-thinking puzzle\n/resetpuzzle - reset the current puzzle's conversation and attempt count\n/adminclearpuzzles - (admin only) clear all puzzle sessions and completed-puzzle history");
            }

            return Ok();
        }

        await SendPuzzleResponseAsync(chat.Id,
            token => lateralPuzzleService.AskAsync(chat.Id, user.Id, text, token));
        return Ok();
    }

    private async Task SendPuzzleResponseAsync(long chatId, Func<CancellationToken, Task<string>> getResponse)
    {
        var chatLock = ChatLocks.GetOrAdd(chatId, _ => new SemaphoreSlim(1, 1));
        await chatLock.WaitAsync(HttpContext.RequestAborted);
        try
        {
            var response = await getResponse(HttpContext.RequestAborted);
            await botClient.Client.SendMessage(chatId, response, cancellationToken: HttpContext.RequestAborted);
        }
        finally
        {
            chatLock.Release();
        }
    }
}
