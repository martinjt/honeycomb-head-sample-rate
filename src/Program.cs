using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<AlwaysOnSampler>();
builder.Services.AddSingleton(sp => new HealthCheckSampler<AlwaysOnSampler>(100, 
    sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<AlwaysOnSampler>()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("tracestate-samplerate-test"))
    .WithTracing(tpb => tpb
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());

builder.Services.ConfigureOpenTelemetryTracerProvider((sp, tpb) =>{
    tpb.AddSource("blah");
    tpb.SetSampler(sp.GetRequiredService<HealthCheckSampler<AlwaysOnSampler>>());
}
);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapHealthChecks("/health");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}