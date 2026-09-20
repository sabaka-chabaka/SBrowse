namespace SBrowse.Parser.AST;

public record NumberLit(int Value, Span Span) : IExpr;
public record StringLit(string Value, Span Span) : IExpr;
public record ColorLit(string Value, Span Span) : IExpr;

public record BlockStatement(string Identifier, List<IStmt> Statements, Span Span) : IStmt;
public record PropertyStatement(string Identifier, IExpr Value, Span Span) : IStmt;

public record IdentifierExpr(string Name, Span Span) : IExpr;