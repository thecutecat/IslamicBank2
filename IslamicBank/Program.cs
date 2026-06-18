using IslamicBank.Data;
using IslamicBank.Infrastructure;
using IslamicBank.Library;
using IslamicBank.Repositories;
using IslamicBank.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

//context provider
builder.Services.AddScoped<IActionContextProvider, ActionContextProvider>();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AuditLogFilter>();
});

builder.Services.AddDbContext<IslamicBankDbContext>(Options =>
{
    Options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."))
    .EnableSensitiveDataLogging()
    .EnableDetailedErrors();
} );

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Connection string 'Redis' not found."), true);
    return ConnectionMultiplexer.Connect(configuration);
});


builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IDistributedLockService, RedisLockService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

// Add other services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<AuditLogFilter>();
builder.Services.AddScoped<IAuditService, AuditService>();

//busines services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IMurabahaService, MurabahaService>();
builder.Services.AddScoped<IProfitDistributionService, ProfitDistributionService>();

// Background service for monthly profit distribution
builder.Services.AddHostedService<ProfitDistributionBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
 
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets(); //app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
