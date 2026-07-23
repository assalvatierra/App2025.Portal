using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.DBLayer;
using Portal.DBServices;
using Portal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Register 3-layer architecture dependencies
builder.Services.AddScoped<IPortalConfigurationDbLayer, PortalConfigurationDbLayer>();
builder.Services.AddScoped<IPortalConfigurationService, PortalConfigurationService>();
builder.Services.AddScoped<IPortalItemDbLayer, PortalItemDbLayer>();
builder.Services.AddScoped<IPortalItemService, PortalItemService>();
builder.Services.AddScoped<IPortalItemSpecDbLayer, PortalItemSpecDbLayer>();
builder.Services.AddScoped<IPortalItemSpecService, PortalItemSpecService>();
builder.Services.AddScoped<IPortalReservationDbLayer, PortalReservationDbLayer>();
builder.Services.AddScoped<IPortalReservationService, PortalReservationService>();
builder.Services.AddScoped<IPortalCategoryDbLayer, PortalCategoryDbLayer>();
builder.Services.AddScoped<IPortalCategoryServices, PortalCategoryServices>();
builder.Services.AddScoped<IPortalContentDbLayer, PortalContentDbLayer>();
builder.Services.AddScoped<IPortalContentService, PortalContentService>();
builder.Services.AddScoped<IEmailService, EmailServiceV2>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

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

// Enable session middleware
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
