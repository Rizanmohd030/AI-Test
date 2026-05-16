using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// QuestPDF Community License (free for revenue < $1M)
QuestPDF.Settings.License = LicenseType.Community;

// Load .env file into environment variables
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// --- DATABASE (reads from .env) ---
var connectionString = $"Host={Env("DB_HOST")};Port={Env("DB_PORT")};Database={Env("DB_NAME")};Username={Env("DB_USER")};Password={Env("DB_PASSWORD")}";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- GROQ AI CONFIG (API key rotation system) ---
builder.Services.AddSingleton<IKeyRotationService, KeyRotationService>();

// --- SERVICES ---
builder.Services.AddHttpClient<IGroqService, GroqService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<IPdfService, PdfService>();

// --- CONTROLLERS + SWAGGER ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- CORS (allow Next.js frontend) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- MIDDLEWARE PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.MapControllers();

// --- AUTO-MIGRATE DATABASE ON STARTUP ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

// Helper to read environment variables with a clear error
static string Env(string key) =>
    Environment.GetEnvironmentVariable(key)
    ?? throw new InvalidOperationException($"Environment variable '{key}' is not set. Check your .env file.");
