using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using RetroMask.API.Authorization;
using RetroMask.API.Middleware;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Mapping;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Infrastructure;
using RetroMask.Infrastructure.Identity;
using RetroMask.Infrastructure.Persistence;
using RetroMask.Infrastructure.Realtime;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (DB, Identity, JWT, SignalR, AI, …) ────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Application ──────────────────────────────────────────────────────────
builder.Services.AddAutoMapper(_ => { }, typeof(MappingProfile).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.Load("RetroMask.Application"));

// ── HTTP Context & CurrentUser ────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ── Controllers & Swagger ────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "RetroMask API",
        Version = "v1",
        Description = "REST API for RetroMask - a collaborative retrospective platform with real-time voting, " +
                      "icebreaker games, anonymous feedback, AI-driven insights, and personal growth tracking."
    });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {token}"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ── CORS ─────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ── Migrate & Seed ────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RetroMaskDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await IdentitySeeder.SeedAsync(userManager, roleManager, logger);
}

// ── Middleware pipeline ───────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");

var uploadsPath = builder.Configuration["FileStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.GetFullPath(uploadsPath)),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SessionHub>("/hubs/session");

app.Run();

public partial class Program;
