using System.ComponentModel.DataAnnotations;

namespace VesselQuery.Api.Configuration;

public sealed class VesselDataOptions
{
    public const string SectionName = "VesselData";

    [Required]
    public string FilePath { get; set; } = string.Empty;
}
