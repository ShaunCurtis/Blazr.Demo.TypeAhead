namespace Blazr.Demo.TypeAhead;

public record Country
{
    public Guid Uid { get; init; } = Guid.NewGuid();
    public required Guid ContinentUid { get; init; }
    public required string Name { get; init; }
}
