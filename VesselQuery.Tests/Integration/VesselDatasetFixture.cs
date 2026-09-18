using System.Text.RegularExpressions;
using VesselQuery.Core.Data;

namespace VesselQuery.Tests.Integration;

public sealed partial class VesselDatasetFixture
{
    public VesselDatasetFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "vessels.json.gz");
        Store = new InMemoryVesselStore(VesselDataLoader.LoadFromFile(path));
    }

    public InMemoryVesselStore Store { get; }

    public static IReadOnlySet<double> ExpectedQuery1Cvns { get; } = LoadExpectedCvns();

    private static HashSet<double> LoadExpectedCvns()
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "Query1.json"));
        return CvnPattern().Matches(text).Select(m => double.Parse(m.Groups[1].Value)).ToHashSet();
    }

    [GeneratedRegex(@"'X01_CVN':\s*(\d+)")]
    private static partial Regex CvnPattern();
}
