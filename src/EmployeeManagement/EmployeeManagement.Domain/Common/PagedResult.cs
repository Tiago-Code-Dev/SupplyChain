namespace EmployeeManagement.Domain.Common;

/// <summary>
/// Resultado paginado com metadados completos
/// </summary>
public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public bool IsFirstPage { get; init; }
    public bool IsLastPage { get; init; }
    public int FirstItemIndex { get; init; }
    public int LastItemIndex { get; init; }

    private PagedResult(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
        HasPreviousPage = pageNumber > 1;
        HasNextPage = pageNumber < TotalPages;
        IsFirstPage = pageNumber == 1;
        IsLastPage = pageNumber >= TotalPages;
        FirstItemIndex = totalCount > 0 ? (pageNumber - 1) * pageSize + 1 : 0;
        LastItemIndex = Math.Min(pageNumber * pageSize, totalCount);
    }

    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize) => new(items, totalCount, pageNumber, pageSize);

    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10) =>
        new([], 0, pageNumber, pageSize);

    /// <summary>
    /// Mapeia os items para outro tipo
    /// </summary>
    public PagedResult<TDestination> Map<TDestination>(Func<T, TDestination> mapper) =>
        new(
            Items.Select(mapper).ToList(),
            TotalCount,
            PageNumber,
            PageSize);
}