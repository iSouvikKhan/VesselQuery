namespace VesselQuery.Core.Querying;

public static class QueryEvaluator
{
    public static bool Evaluate(QueryNode node, IReadOnlyDictionary<string, object?> row) => node switch
    {
        AndNode and => Evaluate(and.Left, row) && Evaluate(and.Right, row),
        OrNode or => Evaluate(or.Left, row) || Evaluate(or.Right, row),
        NotNode not => !Evaluate(not.Operand, row),
        ComparisonNode comparison => Compare(comparison, row),
        _ => throw new NotSupportedException($"Unsupported query node '{node.GetType().Name}'")
    };

    private static bool Compare(ComparisonNode comparison, IReadOnlyDictionary<string, object?> row)
    {
        if (!row.TryGetValue(comparison.Field, out var actual) || actual is null)
        {
            return false;
        }

        return (actual, comparison.Value) switch
        {
            (double number, double target) => CompareNumbers(number, comparison.Operator, target),
            (string text, string target) => CompareText(text, comparison.Operator, target),
            _ => false
        };
    }

    private static bool CompareNumbers(double actual, ComparisonOperator op, double target) => op switch
    {
        ComparisonOperator.Equal => actual == target,
        ComparisonOperator.NotEqual => actual != target,
        ComparisonOperator.LessThan => actual < target,
        ComparisonOperator.LessThanOrEqual => actual <= target,
        ComparisonOperator.GreaterThan => actual > target,
        ComparisonOperator.GreaterThanOrEqual => actual >= target,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
    };

    private static bool CompareText(string actual, ComparisonOperator op, string target) => op switch
    {
        ComparisonOperator.Equal => string.Equals(actual, target, StringComparison.OrdinalIgnoreCase),
        ComparisonOperator.NotEqual => !string.Equals(actual, target, StringComparison.OrdinalIgnoreCase),
        _ => throw new QueryException($"Operator '{op}' is not supported for text values")
    };
}
