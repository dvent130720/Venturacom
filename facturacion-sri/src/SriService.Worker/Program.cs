using Serilog;
using Shared.Contracts;
using SriService.Worker;
using SriService.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
builder.Services.AddSerilog();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient<ICertificatesClient, CertificatesClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CertificateService:BaseUrl"] ?? "http://certificate-service:8080/");
});

var host = builder.Build();
host.Run();
