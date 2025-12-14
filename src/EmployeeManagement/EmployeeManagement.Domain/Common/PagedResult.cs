namespace EmployeeManagement.Domain.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize) => new(items, totalCount, pageNumber, pageSize);

    public static PagedResult<T> Empty(int pageNumber, int pageSize) =>
        new([], 0, pageNumber, pageSize);
}