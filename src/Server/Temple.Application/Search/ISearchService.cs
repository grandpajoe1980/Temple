namespace Temple.Application.Search;

public interface ISearchService
{
    Task<IReadOnlyList<object>> SearchAsync(Guid tenantId, string query, int limit, CancellationToken ct);
}
