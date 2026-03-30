using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Queue;
using Venturacom.Domain.Enums;
using Venturacom.Infrastructure.Persistence;

namespace Venturacom.Infrastructure.Queue;

public sealed class SqlInvoiceQueue(ApplicationDbContext dbContext) : IInvoiceQueue
{
    public async Task PublishAsync(Guid invoiceJobId, CancellationToken cancellationToken = default)
    {
        await dbContext.InvoiceJobs
            .Where(x => x.Id == invoiceJobId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.ScheduledAtUtc, DateTime.UtcNow)
                .SetProperty(p => p.Status, InvoiceJobStatus.Pending), cancellationToken);
    }

    public async Task<Guid?> ConsumeAsync(CancellationToken cancellationToken = default)
    {
        var claimedId = await dbContext.Database.SqlQueryRaw<Guid>(@"
DECLARE @picked TABLE (Id uniqueidentifier);
UPDATE TOP (1) j WITH (ROWLOCK, READPAST, UPDLOCK)
SET Status = {0}, UpdatedAtUtc = SYSUTCDATETIME()
OUTPUT inserted.Id INTO @picked
FROM invoice_jobs j
WHERE j.Status = {1} AND j.ScheduledAtUtc <= SYSUTCDATETIME()
ORDER BY j.ScheduledAtUtc;
SELECT Id FROM @picked;", (int)InvoiceJobStatus.Processing, (int)InvoiceJobStatus.Pending).FirstOrDefaultAsync(cancellationToken);

        return claimedId == Guid.Empty ? null : claimedId;
    }
}
