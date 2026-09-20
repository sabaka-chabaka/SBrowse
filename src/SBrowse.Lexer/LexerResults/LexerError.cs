using SBrowse.Shared;

namespace SBrowse.Lexer.LexerResults;

public readonly record struct LexerError(string Message, Position Position);