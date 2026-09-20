using SBrowse.Parser.AST;

namespace SBrowse.Parser.ParserResults;

public sealed class ParserResult(IReadOnlyList<IStmt> statements, IReadOnlyList<ParserError> errors)
{
    public IReadOnlyList<IStmt> Stmts { get; } = statements;
    public IReadOnlyList<ParserError> Errors { get; } = errors;
    public bool HasErrors => Errors.Count > 0;
}