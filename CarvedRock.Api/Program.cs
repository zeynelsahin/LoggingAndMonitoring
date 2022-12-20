using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using CarvedRock.Data;
using CarvedRock.Domain;
using Hellang.Middleware.ProblemDetails;
using Microsoft.Data.Sqlite;
using CarvedRock.Api;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
// builder.Logging.AddJsonConsole();
// builder.Logging.AddDebug();
// builder.Services.AddApplicationInsightsTelemetry();

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console().Enrich.WithExceptionDetails().Enrich.FromLogContext().Enrich.With<ActivityEnricher>().WriteTo.Seq("http://localhost:5341");
});
NLog.LogManager.Setup().LoadConfigurationFromFile();
builder.Host.UseNLog();
builder.Services.AddProblemDetails(options =>
{
    options.IncludeExceptionDetails = (context, exception) => false;
    options.OnBeforeWriteDetails = (context, details) =>
    {
        if (details.Status == 500)
        {
            details.Detail = "An error occured in our API. Use the trace id when contacting us.";
        }

        options.Rethrow<SqliteException>();
        options.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
    };
});
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
{
    options.Authority = "https://demo.duendesoftware.com";
    options.Audience = "api";
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        NameClaimType = "email"
    };
});
// builder.Logging.AddFilter("CarvedRock", LogLevel.Debug);
// var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
// var tracePath = Path.Join(path, $"Log_CarvedRock_{DateTime.Now:yyyyMMdd-HHmm}.txt");
// Trace.Listeners.Add(new TextWriterTraceListener(File.CreateText(tracePath)));
// Trace.AutoFlush = true;
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerOptions>(); //Authenticate for Swagger
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IProductLogic, ProductLogic>();
builder.Services.AddDbContext<LocalContext>();
builder.Services.AddScoped<ICarvedRockRepository, CarvedRockRepository>();

builder.Services.AddHealthChecks().AddDbContextCheck<LocalContext>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LocalContext>();
    context.MigrateAndCreateData();
}

app.UseMiddleware<CriticalExceptionMiddleware>();
app.UseProblemDetails();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("interactive.public.short");
        options.OAuthAppName("CarvedRock API");
        options.OAuthUsePkce();
    });
}

app.MapFallback(() => Results.Redirect("/swagger"));

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<UseScopeMiddleware>();
app.UseAuthorization();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();
app.Run();