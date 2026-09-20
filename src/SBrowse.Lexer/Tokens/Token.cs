using SBrowse.Shared;

namespace SBrowse.Lexer.Tokens;

public readonly record struct Token(TokenType Type, string Value, Position Start, Position End)
{
    public override string ToString() => $"{Type}({Value}) at {Start.Line}:{Start.Column}";
}