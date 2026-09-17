using FlakeGuard.Api.Endpoints;
using FlakeGuard.Api.Infrastructure;
using FlakeGuard.Api.Workers;
using FlakeGuard.Core.Infrastructure.Postgres;
using FlakeGuard.Core.Repositories;
using FlakeGuard.Core.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres.");

builder.Services.AddDbContext<FlakeGuardDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IRepositoryRepository, PostgresRepositoryRepository>();
builder.Services.AddScoped<ITestCaseRepository, PostgresTestCaseRepository>();
builder.Services.AddScoped<ITestRunRepository, PostgresTestRunRepository>();
builder.Services.AddScoped<IOutboxRepository, PostgresOutboxRepository>();

builder.Services.AddScoped<TestCaseRegistry>();
builder.Services.AddScoped<TestRunProcessor>();
builder.Services.AddSingleton<FlakinessScorer>();
builder.Services.AddSingleton<OutboxSignal>();

builder.Services.AddHttpClient<IQuarantineNotifier, GitHubQuarantineNotifier>();
builder.Services.AddHostedService<OutboxWorker>();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlakeGuardDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapRepositoryEndpoints();
app.MapIngestionEndpoints();

app.Run();

public partial class Program;
