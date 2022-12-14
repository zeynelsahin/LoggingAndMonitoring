using Microsoft.AspNetCore.HttpLogging;
using System.IdentityModel.Tokens.Jwt;
using CarvedRock.WebApp;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
// builder.Logging.AddJsonConsole();
// builder.Services.AddApplicationInsightsTelemetry();
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console().Enrich.WithExceptionDetails().Enrich.FromLogContext().Enrich.With<ActivityEnricher>().WriteTo.Seq("http://localhost:5341");
});
//Open Telemetry
builder.Services.AddOpenTelemetry().WithTracing(providerBuilder =>
{
    providerBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(builder.Environment.ApplicationName)).AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddOtlpExporter(builder =>
    {
        builder.Endpoint = new Uri("http://localhost:4317");
    });
});
// NLog.LogManager.Setup().LoadConfigurationFromFile();
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
builder.Services.AddHealthChecks().AddIdentityServer(new Uri("https://demo.duendesoftware.com"),failureStatus:HealthStatus.Degraded);
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
app.UseExceptionHandler("/Error");
// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<UserScopeMiddleware>();
app.UseAuthorization();
app.MapRazorPages().RequireAuthorization();
app.MapHealthChecks("health").AllowAnonymous();
app.Run();