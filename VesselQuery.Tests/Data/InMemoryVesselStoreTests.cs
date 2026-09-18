using VesselQuery.Core.Data;

namespace VesselQuery.Tests.Data;

public class InMemoryVesselStoreTests
{
    [Fact]
    public void Field_names_are_collected_from_every_row_in_order()
    {
        var store = new InMemoryVesselStore(
        [
            new Dictionary<string, object?> { ["A"] = 1d, ["B"] = 2d },
            new Dictionary<string, object?> { ["B"] = 3d, ["C"] = 4d }
        ]);

        Assert.Equal(["A", "B", "C"], store.FieldNames);
        Assert.True(store.HasField("c"));
        Assert.False(store.HasField("D"));
    }
}
