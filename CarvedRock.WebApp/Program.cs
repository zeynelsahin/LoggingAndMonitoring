using Microsoft.AspNetCore.HttpLogging;
using System.IdentityModel.Tokens.Jwt;
using CarvedRock.WebApp;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using Serilog;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
// builder.Logging.AddJsonConsole();
// builder.Services.AddApplicationInsightsTelemetry();
// builder.Host.UseSerilog((context, configuration) =>
// {
//     configuration.WriteTo.Console().Enrich.WithExceptionDetails().WriteTo.Seq("http://localhost:5341");
// });
NLog.LogManager.Setup().LoadConfigurationFromFile();
builder.Host.UseNLog();
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies")
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
        options.ClientId = "interactive.confidential";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("api");
        options.Scope.Add("offline_access");
        options.GetClaimsFromUserInfoEndpoint = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "email"
        };
        options.SaveTokens = true;
    });

builder.Services.AddHttpContextAccessor();
// builder.Services.AddHttpLogging(options =>
// {
//     options.LoggingFields = HttpLoggingFields.All;
//     options.MediaTypeOptions.AddText("application/json");
//     options.RequestBodyLogLimit = 4096;
//     options.RequestBodyLogLimit = 4096;
// });

var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
builder.Services.AddW3CLogging(options =>
{
    options.LoggingFields = W3CLoggingFields.All;
    options.FileSizeLimit = 5 * 1024 * 1024;
    options.RetainedFileCountLimit = 2;
    options.FileName = "CarvedRock-W3C-UI";
    options.LogDirectory = Path.Combine(path, "logs");
    options.FlushInterval = TimeSpan.FromSeconds(2);
});
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

var app = builder.Build();

// app.UseHttpLogging();
app.UseW3CLogging();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<UserScopeMiddleware>();
app.UseAuthorization();
app.MapRazorPages().RequireAuthorization();

app.Run();