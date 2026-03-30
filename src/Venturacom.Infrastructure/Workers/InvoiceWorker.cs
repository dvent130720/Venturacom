using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Venturacom.Application.Abstractions.Queue;
using Venturacom.Domain.Entities;
using Venturacom.Domain.Enums;
using Venturacom.Infrastructure.Persistence;
using Venturacom.Infrastructure.Services;

namespace Venturacom.Infrastructure.Workers;

public sealed class InvoiceWorker(IServiceProvider serviceProvider, IInvoiceQueue queue, ILogger<InvoiceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var jobId = await queue.ConsumeAsync(stoppingToken);
            if (jobId is null) continue;

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sriService = scope.ServiceProvider.GetRequiredService<ISriXmlService>();

            var job = await dbContext.InvoiceJobs.Include(x => x.Invoice).FirstOrDefaultAsync(x => x.Id == jobId, stoppingToken);
            if (job is null) continue;

            job.Status = InvoiceJobStatus.Processing;
            job.Invoice.Status = InvoiceStatus.Processing;
            await dbContext.SaveChangesAsync(stoppingToken);

            try
            {
                var requestXml = sriService.GenerateInvoiceXml(job.Invoice);
                await dbContext.SriLogs.AddAsync(new SriLog { TenantId = job.TenantId, InvoiceId = job.InvoiceId, Direction = "OUT", XmlContent = requestXml }, stoppingToken);

                var result = await sriService.SendAsync(requestXml, stoppingToken);
                await dbContext.SriLogs.AddAsync(new SriLog { TenantId = job.TenantId, InvoiceId = job.InvoiceId, Direction = "IN", XmlContent = result.ResponseXml }, stoppingToken);

                job.Status = result.Authorized ? InvoiceJobStatus.Authorized : InvoiceJobStatus.Rejected;
                job.Invoice.Status = result.Authorized ? InvoiceStatus.Authorized : InvoiceStatus.Rejected;
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                job.RetryCount++;
                job.LastError = ex.Message;
                job.Status = InvoiceJobStatus.Pending;
                job.Invoice.Status = InvoiceStatus.Pending;
                job.ScheduledAtUtc = DateTime.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, job.RetryCount)));
                await dbContext.SaveChangesAsync(stoppingToken);

                _ = Task.Run(async () =>
                {
                    await Task.Delay(job.ScheduledAtUtc - DateTime.UtcNow, stoppingToken);
                    await queue.PublishAsync(job.Id, stoppingToken);
                }, stoppingToken);

                logger.LogWarning(ex, "Invoice job {JobId} failed", job.Id);
            }
        }
    }
}
