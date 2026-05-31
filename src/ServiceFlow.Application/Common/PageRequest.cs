namespace ServiceFlow.Application.Common;

public sealed record PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public PageRequest(int page = DefaultPage, int pageSize = DefaultPageSize)
    {
        Page = page <= 0 ? DefaultPage : page;
        PageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;
}
