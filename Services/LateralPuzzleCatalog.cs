using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using YensTelegramLuckyDraw.Models.Settings;

namespace YensTelegramLuckyDraw.Services;

public sealed class LateralPuzzleCatalog
{
    private static readonly Regex PuzzleRegex = new(
        @"\*\*(?<number>\d+)\.\*\*\s*(?<question>.*?)\s*<details><summary>Answer</summary>(?<answer>.*?)</details>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public LateralPuzzleCatalog(IWebHostEnvironment environment, IOptions<LateralThinkingSettings> options)
    {
        var fileName = options.Value.FileName;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("LateralThinkingSettings:FileName must be configured.");
        }

        var path = Path.Combine(environment.ContentRootPath, "Puzzles", fileName);
        if (!File.Exists(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, "Puzzles", fileName);
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Lateral thinking puzzle file was not found.", path);
        }

        var markdown = File.ReadAllText(path);
        Puzzles = PuzzleRegex.Matches(markdown)
            .Select(match => new LateralPuzzle(
                int.Parse(match.Groups["number"].Value),
                match.Groups["question"].Value.Trim(),
                match.Groups["answer"].Value.Trim()))
            .ToArray();

        if (Puzzles.Count == 0)
        {
            throw new InvalidDataException("No puzzles with answers were found in the markdown file.");
        }
    }

    public IReadOnlyList<LateralPuzzle> Puzzles { get; }
}

public sealed record LateralPuzzle(int Number, string Question, string Answer);
