using System.ComponentModel.DataAnnotations;

namespace VesselQuery.Api.Contracts;

public sealed record QueryRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Query { get; init; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Skip { get; init; }

    [Range(1, MaxTake)]
    public int Take { get; init; } = 100;

    public const int MaxTake = 1000;
}
