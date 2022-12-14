using System.Diagnostics;
using CarvedRock.Api;
using CarvedRock.Data;
using CarvedRock.Domain;
using Hellang.Middleware.ProblemDetails;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
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
// builder.Logging.AddFilter("CarvedRock", LogLevel.Debug);
// var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
// var tracePath = Path.Join(path, $"Log_CarvedRock_{DateTime.Now:yyyyMMdd-HHmm}.txt");
// Trace.Listeners.Add(new TextWriterTraceListener(File.CreateText(tracePath)));
// Trace.AutoFlush = true;
// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IProductLogic, ProductLogic>();

builder.Services.AddDbContext<LocalContext>();
builder.Services.AddScoped<ICarvedRockRepository, CarvedRockRepository>();

var app = builder.Build();

app.UseMiddleware<CriticalExceptionMiddleware>();
app.UseProblemDetails();
using (var scope= app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LocalContext>();
    context.MigrateAndCreateData();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapFallback(() => Results.Redirect("/swagger"));

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();
