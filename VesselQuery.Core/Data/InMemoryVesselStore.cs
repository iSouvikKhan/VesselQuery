namespace VesselQuery.Core.Data;

public sealed class InMemoryVesselStore : IVesselStore
{
    private readonly HashSet<string> _fieldLookup;

    public InMemoryVesselStore(IReadOnlyList<IReadOnlyDictionary<string, object?>> vessels)
    {
        ArgumentNullException.ThrowIfNull(vessels);

        Vessels = vessels;

        var fieldNames = new List<string>();
        _fieldLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in vessels.SelectMany(row => row.Keys))
        {
            if (_fieldLookup.Add(name))
            {
                fieldNames.Add(name);
            }
        }

        FieldNames = fieldNames;
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Vessels { get; }

    public IReadOnlyList<string> FieldNames { get; }

    public bool HasField(string name) => _fieldLookup.Contains(name);
}
