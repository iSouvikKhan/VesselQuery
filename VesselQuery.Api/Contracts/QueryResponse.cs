namespace VesselQuery.Api.Contracts;

public sealed record QueryResponse(
    string Query,
    int Total,
    int Skip,
    int Take,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Results);
