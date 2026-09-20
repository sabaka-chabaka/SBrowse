using System.Globalization;
using SBrowse.Lexer.Tokens;
using SBrowse.Parser.AST;
using SBrowse.Parser.Interfaces;
using SBrowse.Parser.ParserResults;
using SBrowse.Shared;

namespace SBrowse.Parser;

public class Parser(IReadOnlyList<Token> tokens) : IParser
{
    private int _pos;
    private readonly List<ParserError> _errors = [];

    public ParserResult Parse()
    {
        _pos = 0;
        _errors.Clear();
        var stmts = new List<IStmt>();

        while (!IsAtEnd)
        {
            try
            {
                stmts.Add(ParseStatement());
            }
            catch (ParseException)
            {
                Synchronize();
            }
        }

        return new ParserResult(stmts, _errors);
    }

    private IStmt ParseStatement()
    {
        if (Current.Type != TokenType.Identifier)
        {
            AddError($"Unexpected token '{Current.Value}'. Expected identifier.", Current.Start);
            throw new ParseException();
        }

        var next = Peek(1);
        if (next.Type == TokenType.LBrace)
        {
            return ParseBlockStatement();
        }

        if (next.Type == TokenType.Colon)
        {
            return ParsePropertyStatement();
        }

        AddError($"Expected ':' or '{{' after identifier '{Current.Value}'.", next.Start);
        throw new ParseException();
    }

    private BlockStatement ParseBlockStatement()
    {
        var idToken = Consume(TokenType.Identifier, "Expected identifier.");
        Consume(TokenType.LBrace, "Expected '{'.");
        var body = new List<IStmt>();

        while (!IsAtEnd && !Check(TokenType.RBrace))
        {
            try
            {
                body.Add(ParseStatement());
            }
            catch (ParseException)
            {
                Synchronize();
            }
        }

        var rBrace = Consume(TokenType.RBrace, "Expected '}'.");
        var span = new Span(idToken.Start, rBrace.End);
        return new BlockStatement(idToken.Value, body, span);
    }

    private PropertyStatement ParsePropertyStatement()
    {
        var idToken = Consume(TokenType.Identifier, "Expected identifier.");
        Consume(TokenType.Colon, "Expected ':'.");
        var expr = ParseExpression();
        var semicolon = Consume(TokenType.Semicolon, "Expected ';'.");
        var span = new Span(idToken.Start, semicolon.End);
        return new PropertyStatement(idToken.Value, expr, span);
    }

    private IExpr ParseExpression()
    {
        var token = Current;
        switch (token.Type)
        {
            case TokenType.NumberLiteral:
                Advance();
                if (int.TryParse(token.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
                {
                    return new NumberLit(intVal, new Span(token.Start, token.End));
                }
                AddError($"Invalid integer: '{token.Value}'.", token.Start);
                throw new ParseException();

            case TokenType.StringLiteral:
                Advance();
                return new StringLit(token.Value, new Span(token.Start, token.End));

            case TokenType.ColorLiteral:
                Advance();
                return new ColorLit(token.Value, new Span(token.Start, token.End));

            case TokenType.Identifier:
                Advance();
                return new IdentifierExpr(token.Value, new Span(token.Start, token.End));

            default:
                AddError($"Expected expression, but found '{token.Value}'.", token.Start);
                throw new ParseException();
        }
    }

    private bool IsAtEnd => _pos >= tokens.Count || Current.Type == TokenType.Eof;

    private Token Current => _pos < tokens.Count
        ? tokens[_pos]
        : (tokens.Count > 0 && tokens[^1].Type == TokenType.Eof
            ? tokens[^1]
            : new Token(TokenType.Eof, "", default, default));

    private Token Peek(int offset = 1)
    {
        var index = _pos + offset;
        if (index >= tokens.Count)
        {
            return tokens.Count > 0 && tokens[^1].Type == TokenType.Eof
                ? tokens[^1]
                : new Token(TokenType.Eof, "", default, default);
        }
        return tokens[index];
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd && type != TokenType.Eof) return false;
        return Current.Type == type;
    }

    private Token Advance()
    {
        var current = Current;
        if (!IsAtEnd)
        {
            _pos++;
        }
        return current;
    }

    private Token Consume(TokenType type, string errorMessage)
    {
        if (Check(type))
        {
            return Advance();
        }

        AddError(errorMessage, Current.Start);
        throw new ParseException();
    }

    private void AddError(string message, Position position)
    {
        _errors.Add(new ParserError(message, position));
    }

    private void Synchronize()
    {
        while (!IsAtEnd)
        {
            if (Current.Type == TokenType.Semicolon)
            {
                Advance();
                return;
            }

            if (Current.Type == TokenType.RBrace)
            {
                return;
            }

            Advance();

            if (Current.Type == TokenType.Identifier)
            {
                return;
            }
        }
    }
}