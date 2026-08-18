namespace Application.Common.Queries;

/// <summary>
/// Data transfer object for query parameters representing pagination and search criteria.
/// </summary>
public sealed class QueryParams
{
    /// <summary>
    /// The page number (starts at 0).
    /// </summary>
    public int? Page { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Optional query string to search/filter the results.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Optional branch ID filter.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Optional category ID filter.
    /// </summary>
    public Guid? CategoryId { get; set; }

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
