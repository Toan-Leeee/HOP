namespace MeetingScheduler.Web.ViewModels.Common;

public class QueryFilter
{
    public string? SearchString { get; set; }
    public string? SortOrder { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
}
public record ServiceResult(bool Succeeded, string? Error = null, int? EntityId = null)
{
    public static ServiceResult Success(int? id = null) => new(true, null, id);
    public static ServiceResult Failure(string error) => new(false, error);
}
public record LookupItem(int Id, string Name);
