namespace VesselQuery.Core.Data;

public interface IVesselStore
{
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Vessels { get; }

    IReadOnlyList<string> FieldNames { get; }

    bool HasField(string name);
}
