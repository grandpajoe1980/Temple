namespace Temple.Application.Terminology;

public interface ITerminologyService
{
    Task<IReadOnlyDictionary<string,string>> GetResolvedAsync(Guid tenantId, string? taxonomyId, CancellationToken ct = default);
}
