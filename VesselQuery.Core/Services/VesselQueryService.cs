using VesselQuery.Core.Data;
using VesselQuery.Core.Querying;

namespace VesselQuery.Core.Services;

public sealed class VesselQueryService(IVesselStore store) : IVesselQueryService
{
    public QueryResult Execute(string query, int skip = 0, int take = int.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegative(take);

        var ast = QueryParser.Parse(query);
        EnsureFieldsExist(ast);

        var total = 0;
        var page = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var vessel in store.Vessels)
        {
            if (!QueryEvaluator.Evaluate(ast, vessel))
            {
                continue;
            }

            if (total >= skip && page.Count < take)
            {
                page.Add(vessel);
            }

            total++;
        }

        return new QueryResult(total, page);
    }

    public IReadOnlyList<string> GetFieldNames() => store.FieldNames;

    private void EnsureFieldsExist(QueryNode node)
    {
        switch (node)
        {
            case ComparisonNode comparison when !store.HasField(comparison.Field):
                throw new QueryException($"Unknown field '{comparison.Field}'");
            case AndNode and:
                EnsureFieldsExist(and.Left);
                EnsureFieldsExist(and.Right);
                break;
            case OrNode or:
                EnsureFieldsExist(or.Left);
                EnsureFieldsExist(or.Right);
                break;
            case NotNode not:
                EnsureFieldsExist(not.Operand);
                break;
        }
    }
}
