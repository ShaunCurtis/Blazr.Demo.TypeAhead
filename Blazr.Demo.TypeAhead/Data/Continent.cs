namespace Blazr.Demo.TypeAhead;

public record Continent
{
    public Guid Uid { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
}
