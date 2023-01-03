/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================

namespace Blazr.Demo.TypeAhead;

public sealed record Continent
{
    public Guid Uid { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
}
