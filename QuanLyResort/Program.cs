using QuanLyResort.Models;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Services;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = "Data Source=(local);Initial Catalog=ResortManagement;Integrated Security=True;Trust Server Certificate=True";

builder.Services.AddDbContext<ResortDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add SignalR
builder.Services.AddSignalR();

// Add Services
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IServiceBookingService, ServiceBookingService>();
builder.Services.AddScoped<IStockIssueService, StockIssueService>();
builder.Services.AddScoped<IMoMoPaymentService, MoMoPaymentService>();
builder.Services.AddMemoryCache();

// Add HttpClient for MoMo payment
builder.Services.AddHttpClient<MoMoPaymentService>();

// Email sender (reads SMTP config from configuration; fallback logs to console)
builder.Services.AddSingleton<IEmailSender>(sp => new EmailSender(builder.Configuration));

// Add Hosted Services
builder.Services.AddHostedService<NightAuditService>();

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Areas routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map SignalR Hub
app.MapHub<HotelHub>("/hotelHub");

// Redirect root to Customer area by default
app.MapGet("/", () => Results.Redirect("/Customer"));

// Redirect /Admin to /Admin/Dashboard
app.MapGet("/Admin", () => Results.Redirect("/Admin/Dashboard"));

// Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ResortDbContext>();
    try
    {
        await QuanLyResort.DataSeeder.SeedDataAsync(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
