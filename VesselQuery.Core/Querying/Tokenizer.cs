using System.Globalization;
using System.Text;

namespace VesselQuery.Core.Querying;

public static class Tokenizer
{
    private static readonly Dictionary<string, TokenKind> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WHERE"] = TokenKind.Where,
        ["AND"] = TokenKind.And,
        ["OR"] = TokenKind.Or,
        ["NOT"] = TokenKind.Not
    };

    private const string SingleQuotes = "'‘’";
    private const string DoubleQuotes = "\"“”";

    public static IReadOnlyList<Token> Tokenize(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tokens = new List<Token>();
        var i = 0;

        while (i < query.Length)
        {
            var c = query[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (char.IsLetter(c) || c == '_')
            {
                tokens.Add(ReadWord(query, ref i));
            }
            else if (IsNumberStart(query, i))
            {
                tokens.Add(ReadNumber(query, ref i));
            }
            else if (SingleQuotes.Contains(c) || DoubleQuotes.Contains(c))
            {
                tokens.Add(ReadString(query, ref i));
            }
            else if (c is '(' or ')')
            {
                tokens.Add(new Token(c == '(' ? TokenKind.LeftParen : TokenKind.RightParen, c.ToString(), null, i));
                i++;
            }
            else if (c is '=' or '<' or '>' or '!')
            {
                tokens.Add(ReadOperator(query, ref i));
            }
            else
            {
                throw new QueryException($"Unexpected character '{c}'", i);
            }
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, null, query.Length));
        return tokens;
    }

    private static Token ReadWord(string query, ref int i)
    {
        var start = i;
        while (i < query.Length && (char.IsLetterOrDigit(query[i]) || query[i] == '_'))
        {
            i++;
        }

        var text = query[start..i];
        var kind = Keywords.TryGetValue(text, out var keyword) ? keyword : TokenKind.Identifier;
        return new Token(kind, text, null, start);
    }

    private static bool IsNumberStart(string query, int i)
    {
        var c = query[i];
        if (char.IsAsciiDigit(c))
        {
            return true;
        }

        var next = i + 1 < query.Length ? query[i + 1] : '\0';
        return (c == '-' && (char.IsAsciiDigit(next) || next == '.'))
            || (c == '.' && char.IsAsciiDigit(next));
    }

    private static Token ReadNumber(string query, ref int i)
    {
        var start = i;
        if (query[i] == '-')
        {
            i++;
        }

        var seenDot = false;
        while (i < query.Length && (char.IsAsciiDigit(query[i]) || (query[i] == '.' && !seenDot)))
        {
            seenDot |= query[i] == '.';
            i++;
        }

        var text = query[start..i];
        if (!double.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var value))
        {
            throw new QueryException($"Invalid number '{text}'", start);
        }

        return new Token(TokenKind.Number, text, value, start);
    }

    private static Token ReadString(string query, ref int i)
    {
        var start = i;
        var family = SingleQuotes.Contains(query[i]) ? SingleQuotes : DoubleQuotes;
        i++;

        var value = new StringBuilder();
        while (i < query.Length)
        {
            var c = query[i];
            if (family.Contains(c))
            {
                if (i + 1 < query.Length && family.Contains(query[i + 1]))
                {
                    value.Append(c);
                    i += 2;
                    continue;
                }

                i++;
                return new Token(TokenKind.String, query[start..i], value.ToString(), start);
            }

            value.Append(c);
            i++;
        }

        throw new QueryException("Unterminated string literal", start);
    }

    private static Token ReadOperator(string query, ref int i)
    {
        var start = i;
        var next = i + 1 < query.Length ? query[i + 1] : '\0';

        var text = (query[i], next) switch
        {
            ('<', '=') => "<=",
            ('>', '=') => ">=",
            ('!', '=') => "!=",
            ('<', '>') => "<>",
            ('!', _) => throw new QueryException("Expected '=' after '!'", start),
            _ => query[i].ToString()
        };

        i += text.Length;
        return new Token(TokenKind.Operator, text, null, start);
    }
}
