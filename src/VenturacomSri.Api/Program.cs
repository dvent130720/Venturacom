using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using VenturacomSri.Api.Data;
using VenturacomSri.Api.Jobs;
using VenturacomSri.Api.Middleware;
using VenturacomSri.Api.Services;
using VenturacomSri.Api.Services.Sri;

// ── Serilog ────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Logging ────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .WriteTo.Console(ctx.HostingEnvironment.IsProduction()
               ? new RenderedCompactJsonFormatter()
               : null!);
    });

    // ── Base de datos (EF Core + PostgreSQL) ───────────────────────
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

    // ── Hangfire (worker background jobs) ─────────────────────────
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

    // ── HTTP Client para SRI ──────────────────────────────────────
    builder.Services.AddHttpClient<SriClientService>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(60);
    });

    // ── Servicios de dominio ──────────────────────────────────────
    builder.Services.AddScoped<XmlGeneratorService>();
    builder.Services.AddScoped<XmlSignerService>();
    builder.Services.AddScoped<CertificateService>();
    builder.Services.AddScoped<InvoiceService>();

    // ── Jobs (necesitan ser resoluble por Hangfire) ───────────────
    builder.Services.AddScoped<SignInvoiceJob>();
    builder.Services.AddScoped<SendInvoiceJob>();
    builder.Services.AddScoped<CheckAuthorizationJob>();

    // ── API ────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    var app = builder.Build();

    // ── Migraciones automáticas al iniciar ────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Migraciones aplicadas correctamente.");
    }

    // ── Pipeline ──────────────────────────────────────────────────
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ApiKeyMiddleware>();

    // Dashboard de Hangfire (solo ambiente de desarrollo)
    if (app.Environment.IsDevelopment())
    {
        app.UseHangfireDashboard("/hangfire");
    }

    app.MapControllers();

    Log.Information("API de facturación SRI iniciada en {Env} | Ambiente SRI: {SriEnv}",
        app.Environment.EnvironmentName,
        builder.Configuration["Sri:Environment"] == "1" ? "PRUEBAS" : "PRODUCCIÓN");

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
