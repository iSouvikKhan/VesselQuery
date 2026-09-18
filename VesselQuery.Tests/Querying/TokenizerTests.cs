using VesselQuery.Core.Querying;

namespace VesselQuery.Tests.Querying;

public class TokenizerTests
{
    [Fact]
    public void Tokenizes_the_sample_query()
    {
        var tokens = Tokenizer.Tokenize("WHERE Z13_STATUS_CODE = 4 AND BUILDER_GROUP = 'Guoyu Logistics'");

        Assert.Equal(
            [
                TokenKind.Where, TokenKind.Identifier, TokenKind.Operator, TokenKind.Number,
                TokenKind.And, TokenKind.Identifier, TokenKind.Operator, TokenKind.String, TokenKind.End
            ],
            tokens.Select(t => t.Kind));
        Assert.Equal(4d, tokens[3].Value);
        Assert.Equal("Guoyu Logistics", tokens[7].Value);
    }

    [Theory]
    [InlineData("'Guoyu Logistics'")]
    [InlineData("\"Guoyu Logistics\"")]
    [InlineData("‘Guoyu Logistics’")]
    [InlineData("“Guoyu Logistics”")]
    [InlineData("‘Guoyu Logistics'")]
    public void Accepts_straight_and_curly_quotes(string literal)
    {
        var token = Tokenizer.Tokenize(literal)[0];

        Assert.Equal(TokenKind.String, token.Kind);
        Assert.Equal("Guoyu Logistics", token.Value);
    }

    [Fact]
    public void Doubled_quote_is_an_escaped_quote()
    {
        var token = Tokenizer.Tokenize("'O''Brien'")[0];

        Assert.Equal("O'Brien", token.Value);
    }

    [Fact]
    public void Quote_from_the_other_family_is_part_of_the_string()
    {
        var token = Tokenizer.Tokenize("\"Peoples' Republic of China\"")[0];

        Assert.Equal("Peoples' Republic of China", token.Value);
    }

    [Theory]
    [InlineData("4", 4d)]
    [InlineData("-4", -4d)]
    [InlineData("42.83333", 42.83333)]
    [InlineData(".5", 0.5)]
    [InlineData("-.5", -0.5)]
    public void Reads_numbers(string text, double expected)
    {
        var token = Tokenizer.Tokenize(text)[0];

        Assert.Equal(TokenKind.Number, token.Kind);
        Assert.Equal(expected, token.Value);
    }

    [Theory]
    [InlineData("=")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("<=")]
    [InlineData(">=")]
    [InlineData("!=")]
    [InlineData("<>")]
    public void Reads_operators(string op)
    {
        var tokens = Tokenizer.Tokenize($"A {op} 1");

        Assert.Equal(TokenKind.Operator, tokens[1].Kind);
        Assert.Equal(op, tokens[1].Text);
    }

    [Fact]
    public void Operators_do_not_need_surrounding_spaces()
    {
        var tokens = Tokenizer.Tokenize("A<=4");

        Assert.Equal(["A", "<=", "4", ""], tokens.Select(t => t.Text));
    }

    [Fact]
    public void Keywords_are_case_insensitive()
    {
        var tokens = Tokenizer.Tokenize("where a = 1 and not b = 2 or c = 3");

        Assert.Contains(tokens, t => t.Kind == TokenKind.Where);
        Assert.Contains(tokens, t => t.Kind == TokenKind.And);
        Assert.Contains(tokens, t => t.Kind == TokenKind.Not);
        Assert.Contains(tokens, t => t.Kind == TokenKind.Or);
    }

    [Fact]
    public void Identifier_that_starts_with_a_keyword_is_still_an_identifier()
    {
        var token = Tokenizer.Tokenize("ORDER_ID")[0];

        Assert.Equal(TokenKind.Identifier, token.Kind);
    }

    [Theory]
    [InlineData("A = 'unterminated", "Unterminated string", 4)]
    [InlineData("A ! 1", "Expected '='", 2)]
    [InlineData("A = 1;", "Unexpected character ';'", 5)]
    public void Reports_errors_with_position(string query, string message, int position)
    {
        var ex = Assert.Throws<QueryException>(() => Tokenizer.Tokenize(query));

        Assert.Contains(message, ex.Message);
        Assert.Equal(position, ex.Position);
    }
}
