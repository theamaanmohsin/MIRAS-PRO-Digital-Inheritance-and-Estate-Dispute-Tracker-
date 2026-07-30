using inheritance_system.Components;
using inheritance_system.Services;
using InheritanceSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace inheritance_system
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration
                    .GetConnectionString("DefaultConnection")));

            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration
                    .GetConnectionString("DefaultConnection")), ServiceLifetime.Scoped);

            // ── Existing Services ─────────────────────────────────
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<DashboardService>();
            builder.Services.AddScoped<CurrentUserService>();
            builder.Services.AddScoped<PropertyService>();
            builder.Services.AddScoped<HeirService>();
            builder.Services.AddScoped<FaraidService>();
            builder.Services.AddScoped<DisputeService>();
            builder.Services.AddScoped<DocumentService>();

            // ── Admin Services ────────────────────────────────────
            builder.Services.AddScoped<AdminService>();
            builder.Services.AddScoped<FrcService>();
            builder.Services.AddScoped<SecuritySettingsService>();
            builder.Services.AddScoped<ComplianceService>();

            // ── HTTP context accessor (needed by some services) ──
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            // Patch existing databases with any missing columns/tables (never drops data)
            try
            {
                DatabaseSchemaInitializer.EnsureAsync(app.Services).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database schema check: {ex.Message}");
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found",
                createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();
            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}