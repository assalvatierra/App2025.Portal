using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.DBLayer;
using Portal.DBServices;
using MassTransit;
using Portal.Services;
using Portal.Services.Caching;
using Portal.Services.MessageBroker;
using Portal.Services.Observability;
using Serilog;
using Portal.Services.VectorSearch;

// Configure Serilog before building the host
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Portal")
    .CreateLogger();

try
{
    Log.Information("Application starting up");
    var builder = WebApplication.CreateBuilder(args);

    /*
     * OBSERVABILITY AND MONITORING CONFIGURATION
     * 
     */
    // Add OpenTelemetry - must be called before building the app
    builder.AddPortalOpenTelemetry();

    // Add services to the container.
    builder.Host.UseSerilog();



    /*
     * DATABASE AND IDENTITY CONFIGURATION
     * 
     */
    // Configure database context with SQL Server
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    // Configure Identity with default settings and require confirmed account
    builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddEntityFrameworkStores<ApplicationDbContext>();


    /*
     * VECTOR DATABASE (Qdrant) CONFIGURATION
     */
    // Configure Qdrant vector search service
    builder.Services.AddQdrant(builder.Configuration);


    /*
     * CACHING CONFIGURATION  
     */
    builder.Services.AddCachingServices(builder.Configuration);
    //builder.Services.AddDistributedMemoryCache();


    // Register RateLimitSettings configuration
    builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimit"));

    // MessageBroker: MassTransit with RabbitMQ, Azure Service Bus, and In-Memory transport options
    // Includess: OutboxService,OutboxPublisher,OutboxPollingService,OutboxCleanupService,DeadLetterNotificationService,IdempotencyService,dempotencyCleanupService
    builder.Services.AddMessageBroker(builder.Configuration);

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
builder.Services.AddScoped<IPortalContentDataDbLayer, PortalContentDataDbLayer>();
builder.Services.AddScoped<IPortalContentDataService, PortalContentDataService>();
builder.Services.AddScoped<IEmailService, EmailServiceV2>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddScoped<ICtaBoxService, CtaBoxService>();
builder.Services.AddScoped<ISemanticKernelService, SemanticKernelServiceOpenAI>();

builder.Services.AddScoped<IVectorService, VectorService>();
builder.Services.AddScoped<IVectorSearchService, QdrantVectorSearchService>();


// VectorDB Scheduler for monitoring and triggering search database refreshes
builder.Services.AddHostedService<VectorDbSchedulerService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient();
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
    Log.Information("Application terminated normally");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}


public partial class Program { }