namespace VesselQuery.Core.Querying;

public sealed class QueryException : Exception
{
    public QueryException(string message, int? position = null)
        : base(position is null ? message : $"{message} (at position {position})")
    {
        Position = position;
    }

    public int? Position { get; }
}
