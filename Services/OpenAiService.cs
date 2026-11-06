using Microsoft.Extensions.AI;

namespace YensTelegramLuckyDraw.Services;

public sealed class OpenAiService
{
  private readonly IChatClient _chatClient;

  public OpenAiService(IChatClient chatClient)
  {
    _chatClient = chatClient;
  }

  public async Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default)
  {
    var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);
    return response.Text;
  }

  public async Task<string> ChatAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default)
  {
    var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
    return response.Text;
  }
}
