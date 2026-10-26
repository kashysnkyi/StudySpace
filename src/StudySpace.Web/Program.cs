using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using StudySpace.Data;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddCors(options => options.AddPolicy("SpaDevelopment", policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<StudySpace.Web.Services.CurrentUserService>();
builder.Services.AddScoped<StudySpace.Web.Services.TenantAccessService>();
builder.Services.AddSingleton<StudySpace.Web.Services.FeatureGateService>();
builder.Services.AddSingleton<StudySpace.Web.Services.DocumentSearchService>();
builder.Services.AddScoped<StudySpace.Web.Services.IAttachmentStorage, StudySpace.Web.Services.AttachmentStorage>();
builder.Services.AddHttpClient<StudySpace.Web.Services.WebhookNotificationService>();
builder.Services.AddHttpClient<StudySpace.Web.Services.AssistantService>();
builder.Services.AddSingleton<StudySpace.Web.Services.IActivityQueue, StudySpace.Web.Services.RabbitActivityQueue>();
builder.Services.AddApplicationInsightsTelemetry();

var authentication = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    });
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddSingleton<StudySpace.Web.Services.ConnectivityPulseService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<StudySpace.Web.Services.ConnectivityPulseService>());
builder.Services.AddSingleton<StudySpace.Web.Services.BoardStateService>();
builder.Services.AddDbContext<StudySpaceDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("StudySpace")));

var app = builder.Build();

var ukrainianCulture = new CultureInfo("uk-UA");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ukrainianCulture),
    SupportedCultures = [ukrainianCulture],
    SupportedUICultures = [ukrainianCulture]
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "StudySpace API";
    options.DisplayRequestDuration();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseWebSockets();
app.UseRouting();
if (app.Environment.IsDevelopment()) app.UseCors("SpaDevelopment");

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapHub<StudySpace.Web.Hubs.CollaborationHub>("/hubs/collaboration");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StudySpaceDbContext>();
    await DatabaseSeeder.SeedAsync(dbContext);
}

app.Run();

public partial class Program;
