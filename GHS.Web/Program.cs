using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using GHS.Web.Data;
using GHS.Web.Services;
using GHS.Web.Services.Core;
using GHS.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// Database
builder.Services.AddDbContextFactory<GHSDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application Services
builder.Services.AddSingleton<TcpConnectionPool>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TcpConnectionPool>());
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<UploadHistoryService>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<LogService>();

// Core Services
builder.Services.AddSingleton<StringProcessor>();
builder.Services.AddSingleton<MaterialClassifier>();
builder.Services.AddSingleton<SelectMatPos>();

// SignalR Hub for real-time communication logs
builder.Services.AddSignalR();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GHSDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapHub<LogHub>("/loghub");
app.MapFallbackToPage("/_Host");

app.Run();
