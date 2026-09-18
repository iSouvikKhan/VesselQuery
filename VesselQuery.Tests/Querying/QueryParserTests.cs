using VesselQuery.Core.Querying;

namespace VesselQuery.Tests.Querying;

public class QueryParserTests
{
    private static ComparisonNode Cmp(string field, ComparisonOperator op, object value) => new(field, op, value);

    [Fact]
    public void Parses_the_sample_query_into_an_and_node()
    {
        var ast = QueryParser.Parse("WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = ‘Guoyu Logistics’");

        Assert.Equal(
            new AndNode(
                Cmp("Z13_STATUS_CODE", ComparisonOperator.Equal, 4d),
                Cmp("BUILDER_GROUP", ComparisonOperator.Equal, "Guoyu Logistics")),
            ast);
    }

    [Theory]
    [InlineData("<", ComparisonOperator.LessThan)]
    [InlineData(">", ComparisonOperator.GreaterThan)]
    [InlineData("=", ComparisonOperator.Equal)]
    [InlineData("<=", ComparisonOperator.LessThanOrEqual)]
    [InlineData(">=", ComparisonOperator.GreaterThanOrEqual)]
    [InlineData("!=", ComparisonOperator.NotEqual)]
    [InlineData("<>", ComparisonOperator.NotEqual)]
    public void Parses_each_comparison_operator(string symbol, ComparisonOperator expected)
    {
        var ast = QueryParser.Parse($"WHERE Z13_STATUS_CODE {symbol} 4");

        Assert.Equal(Cmp("Z13_STATUS_CODE", expected, 4d), ast);
    }

    [Fact]
    public void Where_keyword_is_optional()
    {
        Assert.Equal(QueryParser.Parse("WHERE A = 1"), QueryParser.Parse("A = 1"));
    }

    [Fact]
    public void And_binds_tighter_than_or()
    {
        var ast = QueryParser.Parse("A = 1 OR B = 2 AND C = 3");

        Assert.Equal(
            new OrNode(
                Cmp("A", ComparisonOperator.Equal, 1d),
                new AndNode(Cmp("B", ComparisonOperator.Equal, 2d), Cmp("C", ComparisonOperator.Equal, 3d))),
            ast);
    }

    [Fact]
    public void Parentheses_override_precedence()
    {
        var ast = QueryParser.Parse("(A = 1 OR B = 2) AND C = 3");

        Assert.Equal(
            new AndNode(
                new OrNode(Cmp("A", ComparisonOperator.Equal, 1d), Cmp("B", ComparisonOperator.Equal, 2d)),
                Cmp("C", ComparisonOperator.Equal, 3d)),
            ast);
    }

    [Fact]
    public void Not_applies_to_the_next_factor_only()
    {
        var ast = QueryParser.Parse("NOT A = 1 AND B = 2");

        Assert.Equal(
            new AndNode(new NotNode(Cmp("A", ComparisonOperator.Equal, 1d)), Cmp("B", ComparisonOperator.Equal, 2d)),
            ast);
    }

    [Fact]
    public void Chained_and_is_left_associative()
    {
        var ast = QueryParser.Parse("A = 1 AND B = 2 AND C = 3");

        Assert.Equal(
            new AndNode(
                new AndNode(Cmp("A", ComparisonOperator.Equal, 1d), Cmp("B", ComparisonOperator.Equal, 2d)),
                Cmp("C", ComparisonOperator.Equal, 3d)),
            ast);
    }

    [Theory]
    [InlineData("", "no conditions")]
    [InlineData("WHERE", "no conditions")]
    [InlineData("WHERE A", "Expected a comparison operator")]
    [InlineData("WHERE A =", "Expected a number or a quoted string but the query ended")]
    [InlineData("WHERE 4 = A", "Expected a field name but found '4'")]
    [InlineData("WHERE A = B", "Expected a number or a quoted string but found 'B'")]
    [InlineData("WHERE A = 1 B = 2", "Expected AND, OR or the end of the query but found 'B'")]
    [InlineData("WHERE (A = 1", "Expected ')' but the query ended")]
    [InlineData("WHERE A = 1 AND", "Expected a field name but the query ended")]
    [InlineData("WHERE A < 'text'", "can only be used with numbers")]
    public void Rejects_invalid_queries_with_a_helpful_message(string query, string message)
    {
        var ex = Assert.Throws<QueryException>(() => QueryParser.Parse(query));

        Assert.Contains(message, ex.Message);
    }
}
