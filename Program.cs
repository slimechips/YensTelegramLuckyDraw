using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using Telegram.Bot;
using YensTelegramLuckyDraw.Data;
using YensTelegramLuckyDraw.Models;
using YensTelegramLuckyDraw.Models.Settings;
using YensTelegramLuckyDraw.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// DB: connection string comes from configuration or environment (ConnectionStrings:DefaultConnection)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? builder.Configuration["DefaultConnection"]
                      ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
                      ?? "Server=localhost;Database=YensLuckyDraw;Trusted_Connection=True;TrustServerCertificate=True;";

Console.WriteLine(connectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddHttpLogging(options =>
{
  options.LoggingFields =
    HttpLoggingFields.RequestMethod |
    HttpLoggingFields.RequestPath |
    HttpLoggingFields.RequestQuery |
    HttpLoggingFields.RequestHeaders |
    HttpLoggingFields.ResponseStatusCode |
    HttpLoggingFields.Duration |
    HttpLoggingFields.RequestBody;
});

var openAiSettings = builder.Configuration
    .GetSection("OpenAiSettings")
    .Get<OpenAiSettings>() ?? new OpenAiSettings();
var openAiApiKey = builder.Configuration["OPENAI_API_KEY"] ?? openAiSettings.ApiKey;

if (string.IsNullOrWhiteSpace(openAiApiKey))
{
    throw new InvalidOperationException(
        "OpenAI API key is missing. Configure OpenAiSettings:ApiKey in user secrets or OPENAI_API_KEY.");
}

builder.Services.AddChatClient(
    new OpenAIClient(openAiApiKey)
        .GetChatClient(openAiSettings.ModelName)
        .AsIChatClient());
builder.Services.AddSingleton<OpenAiService>();
builder.Services.Configure<LateralThinkingSettings>(
    builder.Configuration.GetSection("LateralThinkingSettings"));
builder.Services.AddSingleton<LateralPuzzleCatalog>();
builder.Services.AddScoped<LateralPuzzleService>();

// Register Telegram Bot client using token from config or environment
var botToken = builder.Configuration["TELEGRAM_BOT_TOKEN"]
               ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
if (string.IsNullOrWhiteSpace(botToken))
{
    // Register a placeholder client to avoid null DI errors; controller will error if token missing
    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient("") );
}
else
{
    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
}

var app = builder.Build();

app.UseHttpLogging();

// Ensure database exists and seed
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  db.Database.EnsureCreated();
  db.Database.ExecuteSqlRaw("""
      IF OBJECT_ID(N'[LateralPuzzleSessions]', N'U') IS NULL
      BEGIN
          CREATE TABLE [LateralPuzzleSessions] (
              [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LateralPuzzleSessions] PRIMARY KEY,
              [ChatId] bigint NOT NULL,
              [PuzzleNumber] int NOT NULL,
              [PuzzleText] nvarchar(max) NOT NULL,
              [Answer] nvarchar(max) NOT NULL,
              [Attempts] int NOT NULL,
              [IsActive] bit NOT NULL,
              [StartedAt] datetime2 NOT NULL
          );
      END;
      IF OBJECT_ID(N'[LateralPuzzleMessages]', N'U') IS NULL
      BEGIN
          CREATE TABLE [LateralPuzzleMessages] (
              [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LateralPuzzleMessages] PRIMARY KEY,
              [SessionId] int NOT NULL,
              [Sequence] int NOT NULL,
              [Role] nvarchar(max) NOT NULL,
              [Content] nvarchar(max) NOT NULL,
              CONSTRAINT [FK_LateralPuzzleMessages_LateralPuzzleSessions_SessionId]
                  FOREIGN KEY ([SessionId]) REFERENCES [LateralPuzzleSessions] ([Id]) ON DELETE CASCADE
          );
      END;
      IF OBJECT_ID(N'[LateralPuzzleParticipants]', N'U') IS NULL
      BEGIN
          CREATE TABLE [LateralPuzzleParticipants] (
              [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LateralPuzzleParticipants] PRIMARY KEY,
              [SessionId] int NOT NULL,
              [TelegramUserId] bigint NOT NULL,
              CONSTRAINT [FK_LateralPuzzleParticipants_LateralPuzzleSessions_SessionId]
                  FOREIGN KEY ([SessionId]) REFERENCES [LateralPuzzleSessions] ([Id]) ON DELETE CASCADE
          );
          CREATE UNIQUE INDEX [IX_LateralPuzzleParticipants_SessionId_TelegramUserId]
              ON [LateralPuzzleParticipants] ([SessionId], [TelegramUserId]);
      END;
      IF OBJECT_ID(N'[PlayedLateralPuzzles]', N'U') IS NULL
      BEGIN
          CREATE TABLE [PlayedLateralPuzzles] (
              [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PlayedLateralPuzzles] PRIMARY KEY,
              [PuzzleNumber] int NOT NULL,
              [TelegramUserId] bigint NOT NULL,
              [ChatId] bigint NOT NULL,
              [PlayedAt] datetime2 NOT NULL
          );
          CREATE UNIQUE INDEX [IX_PlayedLateralPuzzles_PuzzleNumber_TelegramUserId]
              ON [PlayedLateralPuzzles] ([PuzzleNumber], [TelegramUserId]);
      END;
      """);
  // Seed single draw state for Yen if missing
  if (!db.DrawStates.Any())
  {
    db.DrawStates.Add(new YensTelegramLuckyDraw.Models.DrawState { SpinsAvailable = 0 });
    db.SaveChanges();
  }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
