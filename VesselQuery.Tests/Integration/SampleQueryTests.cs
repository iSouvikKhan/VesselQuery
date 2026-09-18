using VesselQuery.Core.Querying;
using VesselQuery.Core.Services;

namespace VesselQuery.Tests.Integration;

public class SampleQueryTests(VesselDatasetFixture dataset) : IClassFixture<VesselDatasetFixture>
{
    private readonly VesselQueryService _service = new(dataset.Store);

    [Fact]
    public void Dataset_contains_50000_vessels_with_63_fields()
    {
        Assert.Equal(50_000, dataset.Store.Vessels.Count);
        Assert.Equal(63, dataset.Store.FieldNames.Count);
    }

    [Theory]
    [InlineData("WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = ‘Guoyu Logistics’")]
    [InlineData("WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = 'Guoyu Logistics'")]
    [InlineData("where builder_group = \"guoyu logistics\" and z13_status_code = 4")]
    public void Sample_query_returns_exactly_the_rows_in_Query1(string query)
    {
        var result = _service.Execute(query);

        Assert.Equal(18, VesselDatasetFixture.ExpectedQuery1Cvns.Count);
        Assert.Equal(18, result.Total);
        Assert.Equal(
            VesselDatasetFixture.ExpectedQuery1Cvns.Order(),
            result.Items.Select(v => (double)v["X01_CVN"]!).Order());
    }

    [Fact]
    public void Less_equal_and_greater_partition_the_non_null_rows()
    {
        var less = _service.Execute("WHERE Z13_STATUS_CODE < 4").Total;
        var equal = _service.Execute("WHERE Z13_STATUS_CODE = 4").Total;
        var greater = _service.Execute("WHERE Z13_STATUS_CODE > 4").Total;
        var nonNull = dataset.Store.Vessels.Count(v => v["Z13_STATUS_CODE"] is not null);

        Assert.Equal(29_996, equal);
        Assert.True(less > 0);
        Assert.True(greater > 0);
        Assert.Equal(nonNull, less + equal + greater);
    }

    [Fact]
    public void Not_is_the_complement_of_its_operand()
    {
        var query = "BUILDER_GROUP = 'Guoyu Logistics'";
        var matching = _service.Execute(query).Total;
        var notMatching = _service.Execute($"NOT ({query})").Total;
        var nonNull = dataset.Store.Vessels.Count(v => v["BUILDER_GROUP"] is not null);

        Assert.Equal(nonNull, matching + notMatching);
    }

    [Fact]
    public void Paging_returns_a_window_but_reports_the_full_total()
    {
        var all = _service.Execute("WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = 'Guoyu Logistics'");
        var page = _service.Execute("WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = 'Guoyu Logistics'", skip: 5, take: 10);

        Assert.Equal(18, page.Total);
        Assert.Equal(all.Items.Skip(5).Take(10), page.Items);
    }

    [Fact]
    public void Unknown_field_is_rejected()
    {
        var ex = Assert.Throws<QueryException>(() => _service.Execute("WHERE BUILDR_GROUP = 'x'"));

        Assert.Contains("Unknown field 'BUILDR_GROUP'", ex.Message);
    }
}
