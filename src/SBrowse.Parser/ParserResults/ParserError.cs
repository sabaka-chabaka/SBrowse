using SBrowse.Shared;

namespace SBrowse.Parser.ParserResults;

public readonly record struct ParserError(string Message, Position Position);