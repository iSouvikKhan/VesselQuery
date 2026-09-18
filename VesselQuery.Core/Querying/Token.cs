namespace VesselQuery.Core.Querying;

public enum TokenKind
{
    Identifier,
    Number,
    String,
    Operator,
    Where,
    And,
    Or,
    Not,
    LeftParen,
    RightParen,
    End
}

public readonly record struct Token(TokenKind Kind, string Text, object? Value, int Position);
