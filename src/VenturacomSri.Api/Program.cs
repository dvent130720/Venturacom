using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;
using StackExchange.Redis;
using VenturacomSri.Api.Data;
using VenturacomSri.Api.Jobs;
using VenturacomSri.Api.Middleware;
using VenturacomSri.Api.Services;
using VenturacomSri.Api.Services.Auth;
using VenturacomSri.Api.Services.Sri;

// ── Serilog bootstrap ──────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Logging ────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .WriteTo.Console(ctx.HostingEnvironment.IsProduction()
               ? new RenderedCompactJsonFormatter()
               : null!);
    });

    // ── Base de datos (EF Core + PostgreSQL) ───────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

    // ── Redis (sesiones y OTP) ─────────────────────────────────────────────
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(
            builder.Configuration.GetConnectionString("Redis")
            ?? "localhost:6379"));

    // ── JWT Authentication ─────────────────────────────────────────────────
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtSecret  = jwtSection["Secret"]!;

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtSection["Issuer"],
                ValidAudience            = jwtSection["Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(
                                               Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew                = TimeSpan.Zero,
            };
        });

    builder.Services.AddAuthorization();

    // ── CORS ───────────────────────────────────────────────────────────────
    builder.Services.AddCors(opts =>
    {
        opts.AddPolicy("LoginApp", p =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins")
                                               .Get<string[]>()
                          ?? ["http://localhost:4200"];
            p.WithOrigins(origins)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
        });
    });

    // ── Hangfire ───────────────────────────────────────────────────────────
    builder.Services.AddHangfire(cfg =>
        cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
           .UseSimpleAssemblyNameTypeSerializer()
           .UseRecommendedSerializerSettings()
           .UsePostgreSqlStorage(c =>
               c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));

    builder.Services.AddHangfireServer(opts =>
    {
        opts.WorkerCount = 5;
        opts.Queues      = ["default"];
    });

    // ── HTTP Client para SRI ───────────────────────────────────────────────
    builder.Services.AddHttpClient<SriClientService>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(60);
    });

    // ── Servicios de dominio ───────────────────────────────────────────────
    builder.Services.AddScoped<XmlGeneratorService>();
    builder.Services.AddScoped<XmlSignerService>();
    builder.Services.AddScoped<CertificateService>();
    builder.Services.AddScoped<InvoiceService>();

    // ── Servicios de autenticación ─────────────────────────────────────────
    builder.Services.AddScoped<OtpService>();
    builder.Services.AddScoped<EmailService>();
    builder.Services.AddScoped<TokenService>();
    builder.Services.AddScoped<AuthService>();

    // ── Jobs ───────────────────────────────────────────────────────────────
    builder.Services.AddScoped<SignInvoiceJob>();
    builder.Services.AddScoped<SendInvoiceJob>();
    builder.Services.AddScoped<CheckAuthorizationJob>();

    // ── API ────────────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    var app = builder.Build();

    // ── Migraciones automáticas ────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Migraciones aplicadas correctamente.");
    }

    // ── Pipeline ───────────────────────────────────────────────────────────
    app.UseSerilogRequestLogging();
    app.UseCors("LoginApp");
    app.UseAuthentication();
    app.UseAuthorization();

    // ApiKey solo aplica a rutas que no sean /api/auth/** ni /health
    app.UseMiddleware<ApiKeyMiddleware>();

    if (app.Environment.IsDevelopment())
        app.UseHangfireDashboard("/hangfire");

    app.MapControllers();

    Log.Information("API Venturacom iniciada en {Env}", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
