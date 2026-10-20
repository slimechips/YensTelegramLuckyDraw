using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using YensTelegramLuckyDraw.Data;
using YensTelegramLuckyDraw.Models;

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
