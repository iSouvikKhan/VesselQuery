using VesselQuery.Core.Querying;

namespace VesselQuery.Tests.Querying;

public class QueryEvaluatorTests
{
    private static readonly Dictionary<string, object?> Row = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Z13_STATUS_CODE"] = 4d,
        ["BUILDER_GROUP"] = "Guoyu Logistics",
        ["Z80_VESSEL_AGE"] = 42.83333,
        ["L91_HULL_TYPE"] = null
    };

    private static bool Matches(string query) => QueryEvaluator.Evaluate(QueryParser.Parse(query), Row);

    [Theory]
    [InlineData("Z13_STATUS_CODE = 4", true)]
    [InlineData("Z13_STATUS_CODE = 5", false)]
    [InlineData("Z13_STATUS_CODE < 5", true)]
    [InlineData("Z13_STATUS_CODE < 4", false)]
    [InlineData("Z13_STATUS_CODE > 3", true)]
    [InlineData("Z13_STATUS_CODE > 4", false)]
    [InlineData("Z13_STATUS_CODE <= 4", true)]
    [InlineData("Z13_STATUS_CODE >= 4", true)]
    [InlineData("Z13_STATUS_CODE != 4", false)]
    [InlineData("Z13_STATUS_CODE = 4.0", true)]
    [InlineData("Z80_VESSEL_AGE > 42.8", true)]
    public void Compares_numbers(string query, bool expected) => Assert.Equal(expected, Matches(query));

    [Theory]
    [InlineData("BUILDER_GROUP = 'Guoyu Logistics'", true)]
    [InlineData("BUILDER_GROUP = 'guoyu logistics'", true)]
    [InlineData("BUILDER_GROUP = 'Guoyu'", false)]
    [InlineData("BUILDER_GROUP != 'Guoyu'", true)]
    public void Compares_text_case_insensitively(string query, bool expected) => Assert.Equal(expected, Matches(query));

    [Fact]
    public void Field_names_are_case_insensitive() => Assert.True(Matches("z13_status_code = 4"));

    [Theory]
    [InlineData("L91_HULL_TYPE = 'x'")]
    [InlineData("L91_HULL_TYPE != 'x'")]
    [InlineData("L91_HULL_TYPE < 1")]
    public void Null_never_matches(string query) => Assert.False(Matches(query));

    [Theory]
    [InlineData("Z13_STATUS_CODE = '4'")]
    [InlineData("BUILDER_GROUP = 4")]
    public void Type_mismatch_does_not_match(string query) => Assert.False(Matches(query));

    [Theory]
    [InlineData("Z13_STATUS_CODE = 4 AND BUILDER_GROUP = 'Guoyu Logistics'", true)]
    [InlineData("Z13_STATUS_CODE = 4 AND BUILDER_GROUP = 'Other'", false)]
    [InlineData("Z13_STATUS_CODE = 5 OR BUILDER_GROUP = 'Guoyu Logistics'", true)]
    [InlineData("Z13_STATUS_CODE = 5 OR BUILDER_GROUP = 'Other'", false)]
    [InlineData("NOT Z13_STATUS_CODE = 5", true)]
    [InlineData("NOT (Z13_STATUS_CODE = 4 OR BUILDER_GROUP = 'Other')", false)]
    [InlineData("Z13_STATUS_CODE = 5 AND BUILDER_GROUP = 'Other' OR Z80_VESSEL_AGE > 40", true)]
    [InlineData("Z13_STATUS_CODE = 5 AND (BUILDER_GROUP = 'Other' OR Z80_VESSEL_AGE > 40)", false)]
    public void Combines_conditions_with_logic_operators(string query, bool expected) =>
        Assert.Equal(expected, Matches(query));
}
