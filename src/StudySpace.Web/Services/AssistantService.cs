using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StudySpace.Web.Services;

public sealed class AssistantService(HttpClient client, IConfiguration configuration)
{
    public async Task<string> AnswerAsync(string question, IReadOnlyCollection<SearchDocument> documents, CancellationToken cancellationToken)
    {
        var endpoint = configuration["AI:ChatEndpoint"];
        var apiKey = configuration["AI:ApiKey"];
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(apiKey))
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(new
            {
                model = configuration["AI:Model"] ?? "chat-model",
                messages = new object[]
                {
                    new { role = "system", content = "Відповідай українською мовою лише на основі переданого контексту StudySpace. Повідом, якщо контексту недостатньо." },
                    new { role = "user", content = $"Контекст:\n{string.Join("\n", documents.Select(d => $"- {d.Title}: {d.Summary}"))}\n\nЗапитання: {question}" }
                }
            });
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return json.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
                ?? "Налаштована модель повернула порожню відповідь.";
        }

        var relevant = documents.Where(document =>
                document.Title.Contains(question, StringComparison.OrdinalIgnoreCase) ||
                question.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(word =>
                    word.Length > 3 && (document.Title.Contains(word, StringComparison.OrdinalIgnoreCase) || document.Summary.Contains(word, StringComparison.OrdinalIgnoreCase))))
            .Take(3)
            .ToList();
        if (relevant.Count == 0)
        {
            relevant = documents.Take(3).ToList();
        }

        return relevant.Count == 0
            ? "У цьому робочому просторі поки що немає документів. Додайте документ і запитайте ще раз."
            : $"У робочому просторі знайдено корисних документів: {relevant.Count}. " +
              string.Join(" ", relevant.Select(document => $"{document.Title}: {document.Summary}"));
    }
}
