using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.Seed;
using OfficeBooking.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// Application services
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<IAdminReservationService, AdminReservationService>();
builder.Services.AddScoped<IRoomUsageReportService, RoomUsageReportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Always seed roles (Admin, User)
await IdentitySeeder.SeedRolesAsync(app.Services);

// Seed admin only in Development when SEED_ADMIN=true
if (app.Environment.IsDevelopment() &&
    string.Equals(Environment.GetEnvironmentVariable("SEED_ADMIN"), "true", StringComparison.OrdinalIgnoreCase))
{
    await IdentitySeeder.SeedAdminAsync(app.Services);
}

// Seed demo data only in Development when SEED_DEMO_DATA=true
if (app.Environment.IsDevelopment() &&
    string.Equals(Environment.GetEnvironmentVariable("SEED_DEMO_DATA"), "true", StringComparison.OrdinalIgnoreCase))
{
    await DemoDataSeeder.SeedAsync(app.Services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
