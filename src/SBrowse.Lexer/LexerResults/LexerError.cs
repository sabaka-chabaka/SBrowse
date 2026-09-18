using SBrowse.Lexer.Tokens;

namespace SBrowse.Lexer.LexerResults;

public readonly record struct LexerError(string Message, Position Position);