using Microsoft.EntityFrameworkCore;
using Venturacom.Domain.Entities;
using Venturacom.Domain.Entities.Security;

namespace Venturacom.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Product> Products { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<InvoiceJob> InvoiceJobs { get; }
    DbSet<SriLog> SriLogs { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
