using SBrowse.Shared;

namespace SBrowse.Parser.AST;

public readonly record struct Span(Position Start, Position End);