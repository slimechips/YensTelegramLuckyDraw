using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using YensTelegramLuckyDraw.Data;
using YensTelegramLuckyDraw.Models;

namespace YensTelegramLuckyDraw.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITelegramBotClient _botClient;
    private readonly IWebHostEnvironment _env;
  private readonly ILogger<TelegramController> _logger;

    public TelegramController(AppDbContext db, ITelegramBotClient botClient, IWebHostEnvironment env, 
        ILogger<TelegramController> logger)
    {
        _db = db;
        _botClient = botClient;
        _env = env;
        _logger = logger;
    }

    [HttpPost("update")]
    public async Task<IActionResult> ReceiveUpdate([FromBody] Update update)
    {
        if (update?.Message == null) return Ok();

        var message = update.Message;
        var chat = message.Chat;
        if (chat == null) return Ok();

        var text = message.Text?.Trim() ?? string.Empty;
        var from = message.From;

        if (string.IsNullOrWhiteSpace(text)) return Ok();

        if (text.StartsWith("/add_spin", StringComparison.OrdinalIgnoreCase))
        {
            var isAdmin = string.Equals(from?.Username, "Jason", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(from?.FirstName, "Jason", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                await _botClient.SendMessage(chat.Id, "Only admin (Jason) can add spins.");
                return Ok();
            }

            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var count = 1;
            if (parts.Length >= 2 && int.TryParse(parts[1], out var parsed)) count = parsed;

            var state = await _db.DrawStates.FirstOrDefaultAsync();
            if (state == null)
            {
                await _botClient.SendMessage(chat.Id, "Draw state not initialized.");
                return Ok();
            }

            state.SpinsAvailable += count;
            await _db.SaveChangesAsync();

            await _botClient.SendMessage(chat.Id, $"Added {count} spin(s) to Yen. Yen now has {state.SpinsAvailable} spins.");
            return Ok();
        }

        if (text.StartsWith("/spin", StringComparison.OrdinalIgnoreCase))
        {
            var state = await _db.DrawStates.FirstOrDefaultAsync();
            if (state == null)
            {
                await _botClient.SendMessage(chat.Id, "Draw state not initialized.");
                return Ok();
            }

            if (state.SpinsAvailable <= 0)
            {
                await _botClient.SendMessage(chat.Id, "No spins available. Ask Jason to add spins using /add_spin.");
                return Ok();
            }

            state.SpinsAvailable -= 1;

            // Select a prize based on category probabilities and then a prize within the category
            var prize = GetPrize();

            var spin = new SpinResult
            {
                DrawStateId = state.Id,
                Reward = prize.Name,
                Timestamp = DateTime.UtcNow,
                AdminAdded = false
            };

            _db.SpinResults.Add(spin);
            await _db.SaveChangesAsync();

            // Try to send an image for the prize if available in wwwroot/images/prizes/prize{Id}.jpg
            var imagesDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "prizes");
            var filename = $"prize{prize.Id}.jpg";
            var fullPath = Path.Combine(imagesDir, filename);

            if (System.IO.File.Exists(fullPath))
            {
                await using var fs = System.IO.File.OpenRead(fullPath);
                var input = InputFile.FromStream(fs, filename);
                await _botClient.SendPhoto(chat.Id, input, caption: $"{prize.Name} (Spins left: {state.SpinsAvailable})");
            }
            else
            {
                await _botClient.SendMessage(chat.Id, $"Spin result: {prize.Name}. Spins left: {state.SpinsAvailable}");
            }
            return Ok();
        }

        if (text.StartsWith("/see_results", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("See results command received.");
            var state = await _db.DrawStates.FirstOrDefaultAsync();
            if (state == null)
            {
                await _botClient.SendMessage(chat.Id, "Draw state not initialized.");
                return Ok();
            }

            var results = await _db.SpinResults
                .Where(s => s.DrawStateId == state.Id)
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();

            if (!results.Any())
            {
                await _botClient.SendMessage(chat.Id, "No spin results yet.");
                return Ok();
            }

            var lines = results.Take(20).Select(r => $"{r.Timestamp:u}: {r.Reward}");
            var responseText = string.Join("\n", lines);
            await _botClient.SendMessage(chat.Id, responseText);
            return Ok();
        }

        if (text.StartsWith("/", StringComparison.OrdinalIgnoreCase))
        {
            var help = "Commands:\n/spin - use one spin\n/add_spin [n] - (admin Jason only) add n spins to Yen\n/see_results - show recent results";
            await _botClient.SendMessage(chat.Id, help);
            return Ok();
        }

        return Ok();
    }

    private static readonly Random _rng = new();

    private static string GetReward()
    {
        // Probabilities: Very hard 3%, Hard 7%, Medium 12%, Easy 18% (these sum to 40%). Rest -> No Prize
        var r = _rng.NextDouble() * 100.0;
        if (r < 3.0) return "Very hard reward";
        if (r < 3.0 + 7.0) return "Hard reward";
        if (r < 3.0 + 7.0 + 12.0) return "Medium reward";
        if (r < 3.0 + 7.0 + 12.0 + 18.0) return "Easy reward";
        return "No prize";
    }

    private record Prize(int Id, string Name, string Category);

    // Prize pools mapping: category -> prize ids
    private static readonly Prize[] Prizes = new[]
    {
        new Prize(1, "Mofusand Merch (<90 RRP)", "VeryHard"),
        new Prize(2, "Mofusand Merch (<70 RRP)", "Hard"),
        new Prize(3, "Choice of Grab-delivered Drink", "Medium"),
        new Prize(4, "Choice of Truth from Jason (me)", "Medium"),
        new Prize(5, "Choice of Dare from Jason (me)", "Medium"),
        new Prize(6, "Virtual Hug", "Easy"),
        new Prize(7, "1 Virtual Brownie Point", "Easy"),
        new Prize(8, "Choice of snack given at next date", "Easy"),
    };

    private static Prize GetPrize()
    {
        // First pick category by probabilities
        var r = _rng.NextDouble() * 100.0;
        string category;
        if (r < 3.0) category = "VeryHard"; // 3%
        else if (r < 3.0 + 7.0) category = "Hard"; // 7%
        else if (r < 3.0 + 7.0 + 36.0) category = "Medium"; // 36%
        else if (r < 3.0 + 7.0 + 36.0 + 54.0) category = "Easy"; // 18%
        else category = "NoPrize";

        if (category == "NoPrize") return new Prize(0, "No prize", "None");

        var pool = Prizes.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (!pool.Any()) return new Prize(0, "No prize", "None");

        var idx = _rng.Next(0, pool.Length);
        return pool[idx];
    }
}
