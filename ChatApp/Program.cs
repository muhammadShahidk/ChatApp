using ChatApp.Interfaces;
using ChatApp.Models;
using ChatApp.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register our custom services
builder.Services.AddSingleton<ITeamService, TeamService>();
builder.Services.AddSingleton<IChatAssignmentService, ChatAssignmentService>();
builder.Services.AddSingleton<ISessionQueueService, SessionQueueService>();

// Register monitoring services
builder.Services.AddSingleton<MonitoringConfiguration>();
builder.Services.AddSingleton<ISessionPollTracker, SessionPollTrackerService>();
builder.Services.AddSingleton<ISessionMonitorService, SessionMonitorService>();

// Add background service for queue processing
builder.Services.AddHostedService<ChatQueueBackgroundService>();

// Add CORS for frontend development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

// Use CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
