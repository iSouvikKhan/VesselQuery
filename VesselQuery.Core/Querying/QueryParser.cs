namespace VesselQuery.Core.Querying;

public sealed class QueryParser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _index;

    private QueryParser(IReadOnlyList<Token> tokens) => _tokens = tokens;

    public static QueryNode Parse(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var parser = new QueryParser(Tokenizer.Tokenize(query));
        return parser.ParseQuery();
    }

    private Token Current => _tokens[_index];

    private Token Advance() => _tokens[_index++];

    private bool Match(TokenKind kind)
    {
        if (Current.Kind != kind)
        {
            return false;
        }

        _index++;
        return true;
    }

    private Token Expect(TokenKind kind, string description)
    {
        if (Current.Kind != kind)
        {
            throw Unexpected(description);
        }

        return Advance();
    }

    private QueryException Unexpected(string expected) =>
        Current.Kind == TokenKind.End
            ? new QueryException($"Expected {expected} but the query ended", Current.Position)
            : new QueryException($"Expected {expected} but found '{Current.Text}'", Current.Position);

    private QueryNode ParseQuery()
    {
        Match(TokenKind.Where);

        if (Current.Kind == TokenKind.End)
        {
            throw new QueryException("The query has no conditions. Example: WHERE Z13_STATUS_CODE = 4", Current.Position);
        }

        var root = ParseExpression();

        if (Current.Kind != TokenKind.End)
        {
            throw Unexpected("AND, OR or the end of the query");
        }

        return root;
    }

    private QueryNode ParseExpression()
    {
        var left = ParseTerm();
        while (Match(TokenKind.Or))
        {
            left = new OrNode(left, ParseTerm());
        }

        return left;
    }

    private QueryNode ParseTerm()
    {
        var left = ParseFactor();
        while (Match(TokenKind.And))
        {
            left = new AndNode(left, ParseFactor());
        }

        return left;
    }

    private QueryNode ParseFactor()
    {
        if (Match(TokenKind.Not))
        {
            return new NotNode(ParseFactor());
        }

        if (Match(TokenKind.LeftParen))
        {
            var inner = ParseExpression();
            Expect(TokenKind.RightParen, "')'");
            return inner;
        }

        return ParseComparison();
    }

    private ComparisonNode ParseComparison()
    {
        var field = Expect(TokenKind.Identifier, "a field name");
        var opToken = Expect(TokenKind.Operator, "a comparison operator (=, !=, <, <=, >, >=)");
        var op = ToOperator(opToken.Text);

        var literal = Current.Kind is TokenKind.Number or TokenKind.String
            ? Advance()
            : throw Unexpected("a number or a quoted string");

        if (literal.Kind == TokenKind.String && op is not (ComparisonOperator.Equal or ComparisonOperator.NotEqual))
        {
            throw new QueryException(
                $"Operator '{opToken.Text}' can only be used with numbers; text supports = and !=", opToken.Position);
        }

        return new ComparisonNode(field.Text, op, literal.Value!);
    }

    private static ComparisonOperator ToOperator(string text) => text switch
    {
        "=" => ComparisonOperator.Equal,
        "!=" or "<>" => ComparisonOperator.NotEqual,
        "<" => ComparisonOperator.LessThan,
        "<=" => ComparisonOperator.LessThanOrEqual,
        ">" => ComparisonOperator.GreaterThan,
        ">=" => ComparisonOperator.GreaterThanOrEqual,
        _ => throw new ArgumentOutOfRangeException(nameof(text), text, "Unknown operator")
    };
}
