using System.Text.Json.Serialization;
using HelpDeskPro.Data;
using HelpDeskPro.Middleware;
using HelpDeskPro.Models;
using HelpDeskPro.Services.RAG;
using HelpDeskPro.Services.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

// =====================================================================
//  HelpDesk Pro - application start-up
//  Part 1: register services   Part 2: build the pipeline   Part 3: run
// =====================================================================

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Part 1 - services
// ---------------------------------------------------------------------

// Database (PostgreSQL). The connection string is in appsettings.json;
// for your own passwords use User Secrets or environment variables.
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

// Login / roles (ASP.NET Core Identity)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie settings. API calls get 401/403 instead of a redirect to the login page.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;

    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// MVC + API controllers. Every form POST is protected by an anti-forgery token
// (API controllers opt out with [IgnoreAntiforgeryToken] because they use JSON).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
.AddJsonOptions(options =>
{
    // enums are sent/received as text ("Open") instead of numbers (1)
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Swagger = automatic API documentation page (/swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Rate limiting for the chatbot: 20 questions per minute per user
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("chat", httpContext =>
    {
        string userName = httpContext.User.Identity != null && httpContext.User.Identity.IsAuthenticated
            ? httpContext.User.Identity.Name
            : "anonymous";

        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(userName, key =>
            new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

// Our own services
builder.Services.Configure<RagOptions>(builder.Configuration.GetSection("Rag"));
builder.Services.AddSingleton<AppPaths>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<ITextChunker, TextChunker>();
builder.Services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddScoped<IEmbeddingService, LocalEmbeddingService>();
builder.Services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
builder.Services.AddScoped<IKnowledgeSeeder, KnowledgeSeeder>();
builder.Services.AddScoped<IHybridRetriever, HybridRetriever>();
builder.Services.AddScoped<IRerankingService, RerankingService>();
builder.Services.AddScoped<IRAGChatService, RAGChatService>();

// Which "answer writer" do we use?
//   no API key -> ExtractiveLlmService (free, offline)
//   API key    -> OpenAiLlmService
string openAiKey = builder.Configuration["Rag:OpenAiApiKey"];
if (string.IsNullOrWhiteSpace(openAiKey))
{
    builder.Services.AddScoped<ILlmService, ExtractiveLlmService>();
}
else
{
    builder.Services.AddHttpClient<ILlmService, OpenAiLlmService>();
}

// ---------------------------------------------------------------------
// Part 2 - request pipeline (order matters!)
// ---------------------------------------------------------------------

WebApplication app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Simple health check for monitoring tools
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", time = DateTime.UtcNow }));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------------------------------------------------------------------
// Part 3 - create/update the database, add demo data, run
// ---------------------------------------------------------------------

using (IServiceScope scope = app.Services.CreateScope())
{
    ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        bool useMigrations = builder.Configuration.GetValue<bool>("Database:UseMigrations");
        if (useMigrations)
        {
            db.Database.Migrate();          // uses the files in the Migrations folder
        }
        else
        {
            db.Database.EnsureCreated();    // creates all tables if they do not exist
        }

        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database start-up failed. Is PostgreSQL running and is the connection string correct?");
        throw;
    }
}

app.Run();

// Needed so that test projects can start the application
public partial class Program
{
}
