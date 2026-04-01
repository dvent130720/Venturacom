using SRI.Facturacion.Application;
using SRI.Facturacion.Infrastructure;
using SRI.Facturacion.Worker;
var builder=Host.CreateApplicationBuilder(args);
builder.Services.AddApplication(); builder.Services.AddInfrastructure(builder.Configuration); builder.Services.AddHostedService<SriProcessingWorker>();
var host=builder.Build(); host.Run();
