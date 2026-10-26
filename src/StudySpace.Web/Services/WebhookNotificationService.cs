using System.Net.Http.Json;

namespace StudySpace.Web.Services;

public sealed class WebhookNotificationService(HttpClient client, IConfiguration configuration, ILogger<WebhookNotificationService> logger)
{
    public async Task<bool> DocumentPublishedAsync(Guid documentId, string title, CancellationToken cancellationToken)
    {
        var telegramToken = configuration["Integrations:Telegram:BotToken"];
        var telegramChatId = configuration["Integrations:Telegram:ChatId"];
        if (!string.IsNullOrWhiteSpace(telegramToken) && !string.IsNullOrWhiteSpace(telegramChatId))
        {
            var response = await client.PostAsJsonAsync(
                $"https://api.telegram.org/bot{telegramToken}/sendMessage",
                new { chat_id = telegramChatId, text = $"StudySpace published: {title} ({documentId})" },
                cancellationToken);
            return response.IsSuccessStatusCode;
        }

        var webhookUrl = configuration["Integrations:WebhookUrl"];
        if (Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri))
        {
            var response = await client.PostAsJsonAsync(uri, new
            {
                type = "document.published",
                occurredAt = DateTimeOffset.UtcNow,
                data = new { documentId, title }
            }, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        logger.LogInformation("Webhook skipped for published document {DocumentId}; no endpoint is configured.", documentId);
        return false;
    }
}
