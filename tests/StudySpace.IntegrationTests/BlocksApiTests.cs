using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StudySpace.Core.Entities;
using StudySpace.Web.Contracts.Blocks;
using StudySpace.Web.Contracts.Documents;

namespace StudySpace.IntegrationTests;

public sealed class BlocksApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededWorkspaceId = Guid.Parse("e08c2747-1c44-4fde-920f-7f3598e852cf");
    private readonly HttpClient _client;

    public BlocksApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BlocksCanBeCreatedUpdatedReorderedAndDeleted()
    {
        var documentResponse = await _client.PostAsJsonAsync("/api/documents", new CreateDocumentRequest
        {
            WorkspaceId = SeededWorkspaceId,
            Title = $"Block test {Guid.NewGuid():N}",
            Summary = "Temporary integration-test document."
        });
        var document = await documentResponse.Content.ReadFromJsonAsync<DocumentDetailsResponse>();
        Assert.NotNull(document);

        try
        {
            var firstResponse = await _client.PostAsJsonAsync($"/api/documents/{document.Id}/blocks", new CreateBlockRequest
            {
                Type = BlockType.Heading,
                Content = "First block"
            });
            var first = await firstResponse.Content.ReadFromJsonAsync<BlockResponse>();
            Assert.NotNull(first);
            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

            var secondResponse = await _client.PostAsJsonAsync($"/api/documents/{document.Id}/blocks", new CreateBlockRequest
            {
                Type = BlockType.Paragraph,
                Content = "Second block"
            });
            var second = await secondResponse.Content.ReadFromJsonAsync<BlockResponse>();
            Assert.NotNull(second);

            var updateResponse = await _client.PutAsJsonAsync(
                $"/api/documents/{document.Id}/blocks/{first.Id}",
                new UpdateBlockRequest { Type = BlockType.Quote, Content = "Updated first block" });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var reorderResponse = await _client.PutAsJsonAsync(
                $"/api/documents/{document.Id}/blocks/order",
                new ReorderBlocksRequest { BlockIds = [second.Id, first.Id] });
            Assert.Equal(HttpStatusCode.NoContent, reorderResponse.StatusCode);

            var blocks = await _client.GetFromJsonAsync<List<BlockResponse>>($"/api/documents/{document.Id}/blocks");
            Assert.NotNull(blocks);
            Assert.Equal([second.Id, first.Id], blocks.Select(block => block.Id));

            var deleteResponse = await _client.DeleteAsync($"/api/documents/{document.Id}/blocks/{first.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }
        finally
        {
            await _client.DeleteAsync($"/api/documents/{document.Id}");
        }
    }
}

