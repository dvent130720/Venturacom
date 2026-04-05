using CertificateService.Api.Application.Abstractions;
using CertificateService.Api.Application.Certificates.Commands;
using CertificateService.Api.Application.Certificates.Queries;
using CertificateService.Api.Infrastructure.Crypto;
using CertificateService.Api.Infrastructure.Persistence;
using CertificateService.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CertificateDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();
builder.Services.AddScoped<UploadCertificateHandler>();
builder.Services.AddScoped<GetActiveCertificateHandler>();
builder.Services.AddSingleton<CertificateService.Api.Infrastructure.Security.IAuditLogger, CertificateService.Api.Infrastructure.Security.AuditLogger>();

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
    {
        opt.Authority = builder.Configuration["Auth:Authority"];
        opt.Audience = "certificate-service";
        opt.RequireHttpsMetadata = false;
    })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, InternalServiceAuthHandler>("Internal", _ => { });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
