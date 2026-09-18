namespace SBrowse.Lexer.Tokens;

public readonly record struct Position(int Line, int Column, int Offset);