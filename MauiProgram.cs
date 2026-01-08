using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PersonalJournalApp.Data;
using PersonalJournalApp.Entities;
using PersonalJournalApp.Services;
using PersonalJournalApp.Auth;

namespace PersonalJournalApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // Configure SQLite database
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "personaljournalapp.db");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Configure ASP.NET Core Identity for MAUI
            builder.Services.AddIdentityCore<User>(options =>
            {
                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // User settings
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

            // Register services
            builder.Services.AddScoped<JournalService>();
            builder.Services.AddScoped<TagService>();
            builder.Services.AddScoped<CategoryService>();
            builder.Services.AddScoped<AnalyticsService>();
            builder.Services.AddScoped<SettingsService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            builder.Services.AddScoped<CustomAuthStateProvider>(sp =>
                (CustomAuthStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());
            builder.Services.AddAuthorizationCore(options =>
            {
                options.FallbackPolicy = null;
            });

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Initialize database
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DatabaseInitializer.InitializeAsync(context).Wait();
            }

            return app;
        }
    }
}