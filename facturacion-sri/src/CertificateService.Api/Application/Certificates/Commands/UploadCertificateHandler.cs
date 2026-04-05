using CertificateService.Api.Application.Abstractions;
using CertificateService.Api.Domain.Entities;

namespace CertificateService.Api.Application.Certificates.Commands;

public sealed class UploadCertificateHandler(ICertificateRepository repository, IEncryptionService encryption)
{
    public async Task<Guid> HandleAsync(UploadCertificateCommand command, CancellationToken ct)
    {
        await repository.DeactivateByTenantAsync(command.TenantId, ct);

        var entity = new Certificate(
            command.TenantId,
            command.Name,
            encryption.Encrypt(command.P12Bytes),
            encryption.EncryptString(command.Password),
            command.ExpirationDateUtc,
            isActive: true);

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return entity.Id;
    }
}
