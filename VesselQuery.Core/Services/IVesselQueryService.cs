namespace VesselQuery.Core.Services;

public interface IVesselQueryService
{
    QueryResult Execute(string query, int skip = 0, int take = int.MaxValue);

    IReadOnlyList<string> GetFieldNames();
}
