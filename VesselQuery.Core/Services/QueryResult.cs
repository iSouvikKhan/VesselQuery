namespace VesselQuery.Core.Services;

public sealed record QueryResult(int Total, IReadOnlyList<IReadOnlyDictionary<string, object?>> Items);
