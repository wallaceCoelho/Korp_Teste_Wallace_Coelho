namespace Application.Common.Queries;

public sealed class QueryParams
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Search { get; set; }
    public Guid? BranchId { get; set; }

    public string? GetSearchPattern()
    {
        if (string.IsNullOrWhiteSpace(Search))
            return null;

        var cleanTerm = Search.Trim().ToLower();
        return $"%{cleanTerm}%";
    }

    public bool IsPagination() => PageSize is not null and > 0;

    public int? GetPageSize() => IsPagination() ? PageSize : null;

    public int GetSkip()
    {
        if (!IsPagination()) return 0;

        int actualTake = PageSize ?? 1000;
        int actualPage = Page is null or < 1 ? 1 : Page.Value;
        return (actualPage - 1) * actualTake;
    }
}
