using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<NkanyeziLamp.Api.Data.LampDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LampDb")));

builder.Services.Configure<NkanyeziLamp.Api.Models.SpeechOptions>(
    builder.Configuration.GetSection(NkanyeziLamp.Api.Models.SpeechOptions.SectionName));
builder.Services.Configure<NkanyeziLamp.Api.Models.OcrOptions>(
    builder.Configuration.GetSection(NkanyeziLamp.Api.Models.OcrOptions.SectionName));

builder.Services.AddScoped<NkanyeziLamp.Api.Services.PdfExtractionService>();
builder.Services.AddScoped<NkanyeziLamp.Api.Services.QuestionSplitterService>();
builder.Services.AddScoped<NkanyeziLamp.Api.Services.QuestionMemoMatcherService>();
builder.Services.AddScoped<NkanyeziLamp.Api.Services.TeachingIntelligenceService>();
builder.Services.AddScoped<NkanyeziLamp.Api.Services.SlideGeneratorService>();
builder.Services.AddScoped<NkanyeziLamp.Api.Services.BoardScriptService>();
builder.Services.AddScoped<NkanyeziLamp.Api.Services.NarrationScriptService>();
builder.Services.AddScoped<NkanyeziLamp.Api.Services.AudioGeneratorService>();
builder.Services.AddScoped<NkanyeziLamp.Api.Services.VideoRendererService>();
builder.Services.AddScoped<NkanyeziLamp.Api.Services.YouTubeOptimizationService>();
builder.Services.AddSingleton<NkanyeziLamp.Api.Services.WorkflowStateStore>();

// Add configuration for SQLite connection string
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
{
    {"ConnectionStrings:LampDb", "Data Source=lamp.db"}
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminApp", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AdminApp");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NkanyeziLamp.Api.Data.LampDbContext>();
    db.Database.Migrate();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateTime.Now.AddDays(index),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )
    ).ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
