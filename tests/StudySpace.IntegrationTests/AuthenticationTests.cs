using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StudySpace.IntegrationTests;

public sealed class AuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid SeededWorkspaceId = Guid.Parse("e08c2747-1c44-4fde-920f-7f3598e852cf");
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task LoginPageExplainsAvailableProviders()
    {
        using var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/Account/Login");
        Assert.Contains("Вхід до StudySpace", html);
        Assert.Contains("тестовим профілем", html);
    }

    [Fact]
    public async Task TenantWithoutMembershipCannotReadSeededWorkspace()
    {
        using var client = CreateAuthenticatedClient("another-tenant-user");
        var list = await client.GetStringAsync("/Workspaces");
        Assert.DoesNotContain("Ласкаво просимо до StudySpace", list);

        var details = await client.GetAsync($"/Workspaces/Details/{SeededWorkspaceId}");
        Assert.Equal(HttpStatusCode.NotFound, details.StatusCode);
    }

    [Fact]
    public async Task WorkspaceOwnerCanReadSeededWorkspace()
    {
        using var client = CreateAuthenticatedClient("demo-user");
        var details = await client.GetAsync($"/Workspaces/Details/{SeededWorkspaceId}");
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string subject)
    {
        var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
        }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        return client;
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "IntegrationTest";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var subject = Request.Headers["X-Test-Subject"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(subject)) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, subject), new Claim(ClaimTypes.Name, subject)], SchemeName);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }
}
