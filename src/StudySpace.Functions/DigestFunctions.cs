using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace StudySpace.Functions;

public sealed class DigestFunctions(IHttpClientFactory clientFactory)
{
    [Function("Health")]
    public IActionResult Health([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest request) =>
        new OkObjectResult(new { service = "StudySpace.Functions", status = "healthy", utc = DateTimeOffset.UtcNow });

    [Function("DocumentDigest")]
    public async Task<IActionResult> Digest(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "digest")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var apiUrl = Environment.GetEnvironmentVariable("StudySpaceApiUrl")?.TrimEnd('/') ?? "http://localhost:5036";
        var client = clientFactory.CreateClient();
        var page = await client.GetFromJsonAsync<DocumentPage>($"{apiUrl}/api/documents?limit=100", cancellationToken);
        if (page is null) return new StatusCodeResult((int)HttpStatusCode.BadGateway);
        return new OkObjectResult(new
        {
            generatedAt = DateTimeOffset.UtcNow,
            total = page.Total,
            byStatus = page.Items.GroupBy(item => item.Status).ToDictionary(group => group.Key, group => group.Count()),
            recentlyUpdated = page.Items.OrderByDescending(item => item.UpdatedAt).Take(5)
        });
    }

    private sealed record DocumentPage(IReadOnlyList<DocumentItem> Items, int Total);
    private sealed record DocumentItem(Guid Id, string Title, string Status, DateTime UpdatedAt);
}
