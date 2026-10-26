using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StudySpace.Core.Entities;
using StudySpace.Web.Contracts.Documents;

namespace StudySpace.IntegrationTests;

public sealed class DocumentsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededWorkspaceId = Guid.Parse("e08c2747-1c44-4fde-920f-7f3598e852cf");
    private readonly HttpClient _client;

    public DocumentsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CrudLifecycleUsesExpectedStatusCodesAndPagination()
    {
        var title = $"API test {Guid.NewGuid():N}";
        var createRequest = new CreateDocumentRequest
        {
            WorkspaceId = SeededWorkspaceId,
            Title = title,
            Summary = "Created by an integration test.",
            Status = DocumentStatus.Draft
        };

        var createResponse = await _client.PostAsJsonAsync("/api/documents", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var created = await createResponse.Content.ReadFromJsonAsync<DocumentDetailsResponse>();
        Assert.NotNull(created);
        Assert.Equal(title, created.Title);

        var getResponse = await _client.GetAsync($"/api/documents/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var page = await _client.GetFromJsonAsync<PagedResponse<DocumentSummaryResponse>>(
            "/api/documents?skip=0&limit=1");
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.True(page.Total >= 2);
        Assert.NotNull(page.NextLink);

        var updateRequest = new UpdateDocumentRequest
        {
            WorkspaceId = SeededWorkspaceId,
            Title = $"{title} updated",
            Summary = "Updated by an integration test.",
            Status = DocumentStatus.Published
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/documents/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<DocumentDetailsResponse>();
        Assert.NotNull(updated);
        Assert.Equal(DocumentStatus.Published, updated.Status);

        var deleteResponse = await _client.DeleteAsync($"/api/documents/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missingResponse = await _client.GetAsync($"/api/documents/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task CreateRejectsMissingWorkspace()
    {
        var request = new CreateDocumentRequest
        {
            WorkspaceId = Guid.NewGuid(),
            Title = "Valid title",
            Summary = "The workspace is deliberately missing."
        };

        var response = await _client.PostAsJsonAsync("/api/documents", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PaginationRejectsInvalidLimit()
    {
        var response = await _client.GetAsync("/api/documents?limit=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

