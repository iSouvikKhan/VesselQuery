using System.Text;
using VesselQuery.Core.Data;

namespace VesselQuery.Tests.Data;

public class VesselDataLoaderTests
{
    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> Load(string content) =>
        VesselDataLoader.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(content)));

    [Fact]
    public void Strips_the_javascript_wrapper()
    {
        var rows = Load("var vessels = [ { \"A\": 1 }, { \"A\": 2 } ];");

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Loads_a_plain_json_array()
    {
        var rows = Load("[ { \"A\": 1 } ]");

        Assert.Single(rows);
    }

    [Fact]
    public void Normalises_values_to_plain_clr_types()
    {
        var row = Load("""[ { "Int": 4, "Dec": 42.83333, "Text": "x", "Null": null, "Bool": true } ]""")[0];

        Assert.Equal(4d, row["Int"]);
        Assert.Equal(42.83333, row["Dec"]);
        Assert.Equal("x", row["Text"]);
        Assert.Null(row["Null"]);
        Assert.Equal(true, row["Bool"]);
    }

    [Fact]
    public void Field_lookup_is_case_insensitive()
    {
        var row = Load("""[ { "BUILDER_GROUP": "x" } ]""")[0];

        Assert.Equal("x", row["builder_group"]);
    }

    [Fact]
    public void Rejects_content_without_an_array()
    {
        Assert.Throws<InvalidDataException>(() => Load("var vessels = {};"));
    }

    [Fact]
    public void Rejects_an_array_of_non_objects()
    {
        Assert.Throws<InvalidDataException>(() => Load("[1, 2]"));
    }

    [Fact]
    public void Missing_file_gives_a_clear_error()
    {
        var ex = Assert.Throws<FileNotFoundException>(() => VesselDataLoader.LoadFromFile("does-not-exist.json"));

        Assert.Contains("does-not-exist.json", ex.Message);
    }
}
