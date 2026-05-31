using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Services.ActionItems;
using RetroMask.Application.Services.AI;
using RetroMask.Application.Services.Auth;
using RetroMask.Application.Services.Feedback;
using RetroMask.Application.Services.Game;
using RetroMask.Application.Services.Insights;
using RetroMask.Application.Services.Notifications;
using RetroMask.Application.Services.Phases;
using RetroMask.Application.Services.Points;
using RetroMask.Application.Services.Reports;
using RetroMask.Application.Services.Sessions;
using RetroMask.Application.Services.Teams;
using RetroMask.Application.Services.Voting;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Infrastructure.AI;
using RetroMask.Infrastructure.Auth;
using RetroMask.Infrastructure.Email;
using RetroMask.Infrastructure.Files;
using RetroMask.Infrastructure.Persistence;
using RetroMask.Infrastructure.Persistence.Repositories;
using RetroMask.Infrastructure.Realtime;
using RetroMask.Infrastructure.Services.ActionItems;
using RetroMask.Infrastructure.Services.AI;
using RetroMask.Infrastructure.Services.Auth;
using RetroMask.Infrastructure.Services.Feedback;
using RetroMask.Infrastructure.Services.Game;
using RetroMask.Infrastructure.Services.Insights;
using RetroMask.Infrastructure.Services.Notifications;
using RetroMask.Infrastructure.Services.Phases;
using RetroMask.Infrastructure.Services.Points;
using RetroMask.Infrastructure.Services.Reports;
using RetroMask.Infrastructure.Services.Sessions;
using RetroMask.Infrastructure.Services.Teams;
using RetroMask.Infrastructure.Services.Voting;

namespace RetroMask.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────────────────
        services.AddDbContext<RetroMaskDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(RetroMaskDbContext).Assembly.FullName)));

        // ── Identity ─────────────────────────────────────────────────────────
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<RetroMaskDbContext>()
        .AddDefaultTokenProviders();

        // ── JWT ───────────────────────────────────────────────────────────────
        var jwtSettings = configuration.GetSection("JwtSettings");
        services.Configure<JwtSettings>(jwtSettings);
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    // Allow SignalR to pass token via query string
                    var accessToken = ctx.Request.Query["access_token"];
                    var path = ctx.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        ctx.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });

        // ── Repositories & UoW ───────────────────────────────────────────────
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Services ─────────────────────────────────────────────────────────
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ISessionBroadcaster, SessionBroadcaster>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IPhaseService, PhaseService>();
        services.AddScoped<IPointService, PointService>();
        services.AddScoped<IVotingService, VotingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IActionItemService, ActionItemService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IInsightService, InsightService>();

        // ── Email ─────────────────────────────────────────────────────────────
        var useSmtp = configuration.GetValue<bool>("Email:UseSmtp");
        if (useSmtp)
            services.AddScoped<IEmailService, SmtpEmailService>();
        else
            services.AddScoped<IEmailService, ConsoleEmailService>();

        // ── AI ────────────────────────────────────────────────────────────────
        services.AddScoped<IAIInsightProvider, OpenAIInsightProvider>();
        services.AddHttpClient("Gemini");

        // ── SignalR ───────────────────────────────────────────────────────────
        services.AddSignalR();

        return services;
    }
}
