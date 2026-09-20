using System.Text;
using SBrowse.Lexer.Interfaces;
using SBrowse.Lexer.LexerResults;
using SBrowse.Lexer.Tokens;
using SBrowse.Shared;

namespace SBrowse.Lexer;

public class Lexer(string source) : ILexer
{
    private int _offset;
    private int _line = 1;
    private int _column = 1;
    private List<LexerError> _errors = [];
    
    public LexerResult Tokenize()
    {
        _offset = 0;
        _line = 1;
        _column = 1;
        _errors = [];
        var tokens = new List<Token>();

        while (!IsAtEnd())
        {
            SkipWhitespace();
            if (IsAtEnd()) break;

            var token = ReadNextToken();
            if (token.HasValue) tokens.Add(token.Value);
        }
        
        tokens.Add(MakeToken(TokenType.Eof, ""));
        return new LexerResult(tokens, _errors);
    }
    
    private Token? ReadNextToken()
    {
        var start = CurrentPosition();
        var ch = Current();

        if (char.IsDigit(ch) || (ch == '-' && char.IsDigit(Peek()))) return ReadNumber(start);
        if (char.IsLetter(ch) || ch == '_' || ch == '-') return ReadIdentifier(start);
        if (ch == '"') return ReadString(start);
        if (ch == '#') return ReadColor(start);

        return ch switch
        {
            '{' => Consume(TokenType.LBrace,    "{", start),
            '}' => Consume(TokenType.RBrace,    "}", start),
            ';' => Consume(TokenType.Semicolon, ";", start),
            ',' => Consume(TokenType.Comma,     ",", start),
            ':' => Consume(TokenType.Colon,     ":", start),
 
            _   => HandleUnknown(start)
        };
    }

    private Token ReadColor(Position start)
    {
        var sb = new StringBuilder();
        sb.Append(Current());
        Advance();

        while (!IsAtEnd() && char.IsAsciiHexDigit(Current()))
        {
            sb.Append(Current());
            Advance();
        }

        if (sb.Length == 1)
        {
            AddError("Invalid color literal.", start);
        }

        return new Token(TokenType.ColorLiteral, sb.ToString(), start, PreviousPosition());
    }
    
    private Token ReadNumber(Position start)
    {
        var sb = new StringBuilder();
        bool hasDot = false;

        if (Current() == '-')
        {
            sb.Append(Current());
            Advance();
        }

        while (!IsAtEnd() && (char.IsDigit(Current()) || Current() == '.'))
        {
            if (Current() == '.')
            {
                if (hasDot)
                {
                    AddError("Invalid number: multiple dots.", CurrentPosition());
                    break;
                }
                hasDot = true;
            }

            sb.Append(Current());
            Advance();
        }

        return new Token(TokenType.NumberLiteral, sb.ToString(), start, PreviousPosition());
    }
    
    private Token ReadIdentifier(Position start)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd() && (char.IsLetterOrDigit(Current()) || Current() == '_' || Current() == '-'))
        {
            sb.Append(Current());
            Advance();
        }
        
        var text = sb.ToString();
        
        return new Token(TokenType.Identifier, text, start, PreviousPosition());
    }
    
    private Token ReadString(Position start)
    {
        Advance();
        var sb = new StringBuilder();
 
        while (!IsAtEnd() && Current() != '"')
        {
            if (Current() == '\\')
            {
                Advance();
                if (IsAtEnd()) break;

                var escaped = Current() switch
                {
                    '"'  => '"',
                    '\\' => '\\',
                    'n'  => '\n',
                    'r'  => '\r',
                    't'  => '\t',
                    '0'  => '\0',
                    var c => c
                };
                sb.Append(escaped);
            }
            else
            {
                sb.Append(Current());
            }
            Advance();
        }
 
        if (IsAtEnd())
            AddError("Unterminated string literal", start);
        else
            Advance();
 
        return new Token(TokenType.StringLiteral, sb.ToString(), start, PreviousPosition());
    }
    
    private Token Consume(TokenType type, string value, Position start)
    {
        Advance();
        return new Token(type, value, start, PreviousPosition());
    }
    
    private Token? HandleUnknown(Position start)
    {
        var ch = Current();
        Advance();
        AddError($"Unexpected character: '{ch}'", start);
        return null;
    }
    
    private char Current() => _offset < source.Length ? source[_offset] : '\0';

    private char Peek() => _offset + 1 < source.Length ? source[_offset + 1] : '\0';
 
    private void Advance()
    {
        if (_offset >= source.Length) return;
 
        if (source[_offset] == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }
        _offset++;
    }
 
    private void SkipWhitespace()
    {
        while (!IsAtEnd())
        {
            if (char.IsWhiteSpace(Current()))
            {
                Advance();
            }
            else if (Current() == '/' && Peek() == '/')
            {
                Advance();
                Advance();
                while (!IsAtEnd() && Current() != '\n')
                    Advance();
            }
            else if (Current() == '/' && Peek() == '*')
            {
                var commentStart = CurrentPosition();
                Advance();
                Advance();
                bool closed = false;
                while (!IsAtEnd())
                {
                    if (Current() == '*' && Peek() == '/')
                    {
                        Advance();
                        Advance();
                        closed = true;
                        break;
                    }
                    Advance();
                }
                if (!closed)
                {
                    AddError("Unterminated comment", commentStart);
                }
            }
            else
            {
                break;
            }
        }
    }
 
    private bool IsAtEnd() => _offset >= source.Length;
 
    private Position CurrentPosition() => new(_line, _column, _offset);
    
    private Position PreviousPosition()
    {
        if (_offset == 0) return new Position(_line, _column, _offset);

        var col = _column - 1;
        var line = _line;

        if (col <= 0)
        {
            line--;
            col = 1;
        }

        return new Position(line, col, _offset - 1);
    }
    
    private Token MakeToken(TokenType type, string value) =>
        new(type, value, CurrentPosition(), CurrentPosition());

    private void AddError(string message, Position position)
    {
        _errors.Add(new LexerError(message, position));
    }
}