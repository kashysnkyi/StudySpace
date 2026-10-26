using Microsoft.AspNetCore.Mvc.Testing;

namespace StudySpace.IntegrationTests;

public sealed class HomePageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HomePageTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HomePageShowsSeededWorkspace()
    {
        var response = await _client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("StudySpace", html);
        Assert.Contains("Огляд", html);
    }

    [Fact]
    public async Task WorkspaceListIsAvailable()
    {
        var response = await _client.GetAsync("/Workspaces");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Робочі простори", html);
    }

    [Theory]
    [InlineData("/Insights", "Документи за статусом")]
    [InlineData("/robots.txt", "Sitemap:")]
    [InlineData("/sitemap.xml", "urlset")]
    [InlineData("/swagger/index.html", "StudySpace API")]
    public async Task PublicUtilityPagesAreAvailable(string path, string expectedText)
    {
        var response = await _client.GetAsync(path);
        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains(expectedText, content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AutocompleteFindsSeededDocument()
    {
        var response = await _client.GetAsync("/api/search/documents?term=Ласкаво");
        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Ласкаво просимо до StudySpace", content);
    }
}

