using TeamsTimeBot.Api.Services;
using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using TeamsTimeBot.Api.Bots;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<GraphService>();
builder.Services.AddScoped<UserSyncService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHostedService<UserSyncBackgroundService>();
builder.Services.AddScoped<MessageParserService>();
builder.Services.AddScoped<WorkLogService>();
builder.Services.AddScoped<AIService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<BotEngine>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<TaskResolver>();
builder.Services.AddScoped<ActionExecutor>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<ReportService>();


builder.Services.AddSingleton<
    BotFrameworkAuthentication,
    ConfigurationBotFrameworkAuthentication>();

builder.Services.AddSingleton<CloudAdapter, AdapterWithErrorHandler>();
builder.Services.AddTransient<IBot, TeamsBot>();




var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors("Frontend");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();



app.Run();