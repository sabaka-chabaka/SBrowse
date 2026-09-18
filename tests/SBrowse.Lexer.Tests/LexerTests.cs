using SBrowse.Lexer.LexerResults;
using SBrowse.Lexer.Tokens;
using Xunit;

namespace SBrowse.Lexer.Tests;

public class LexerTests
{
    private static List<Token> Tokenize(string source)
    {
        var result = new Lexer(source).Tokenize();
        return [.. result.Tokens];
    }

    private static LexerResult TokenizeFull(string source)
    {
        return new Lexer(source).Tokenize();
    }

    [Fact]
    public void Eof_ReturnsEofToken()
    {
        var tokens = Tokenize("");

        Assert.Single(tokens);
        Assert.Equal(TokenType.Eof, tokens[0].Type);
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("my_variable", "my_variable")]
    [InlineData("font-size", "font-size")]
    [InlineData("_private", "_private")]
    [InlineData("-custom-prop", "-custom-prop")]
    [InlineData("camelCase123", "camelCase123")]
    public void Identifier_ReturnsCorrectToken(string input, string expected)
    {
        var tokens = Tokenize(input);

        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal(expected, tokens[0].Value);
        Assert.Equal(TokenType.Eof, tokens[1].Type);
    }

    [Theory]
    [InlineData("42", "42")]
    [InlineData("0", "0")]
    [InlineData("-42", "-42")]
    [InlineData("3.14", "3.14")]
    [InlineData("-0.5", "-0.5")]
    public void Number_ReturnsCorrectToken(string input, string expected)
    {
        var tokens = Tokenize(input);

        Assert.Equal(TokenType.NumberLiteral, tokens[0].Type);
        Assert.Equal(expected, tokens[0].Value);
    }

    [Theory]
    [InlineData("{", TokenType.LBrace)]
    [InlineData("}", TokenType.RBrace)]
    [InlineData(":", TokenType.Colon)]
    [InlineData(";", TokenType.Semicolon)]
    [InlineData(",", TokenType.Comma)]
    public void Delimiters_ReturnsCorrectToken(string input, TokenType expectedType)
    {
        var tokens = Tokenize(input);

        Assert.Equal(expectedType, tokens[0].Type);
        Assert.Equal(input, tokens[0].Value);
    }

    [Theory]
    [InlineData("#fff", "#fff")]
    [InlineData("#FFFFFF", "#FFFFFF")]
    [InlineData("#12345678", "#12345678")]
    [InlineData("#0", "#0")]
    public void ColorLiteral_ReturnsCorrectToken(string input, string expected)
    {
        var tokens = Tokenize(input);

        Assert.Equal(TokenType.ColorLiteral, tokens[0].Type);
        Assert.Equal(expected, tokens[0].Value);
    }

    [Fact]
    public void ColorLiteral_InvalidWithoutHexDigits_ReportsError()
    {
        var result = TokenizeFull("#");

        Assert.True(result.HasErrors);
        Assert.Single(result.Errors);
        Assert.Equal("Invalid color literal.", result.Errors[0].Message);
    }

    [Fact]
    public void StringLiteral_ReturnsCorrectValue()
    {
        var tokens = Tokenize("\"hello\"");

        Assert.Equal(TokenType.StringLiteral, tokens[0].Type);
        Assert.Equal("hello", tokens[0].Value);
    }

    [Fact]
    public void StringLiteral_Empty_ReturnsEmptyString()
    {
        var tokens = Tokenize("\"\"");

        Assert.Equal(TokenType.StringLiteral, tokens[0].Type);
        Assert.Equal("", tokens[0].Value);
    }

    [Fact]
    public void StringLiteral_WithEscapes_ParsedCorrectly()
    {
        var tokens = Tokenize("\"line1\\nline2\\t\\\"\\\\\\r\\0\\a\"");

        Assert.Equal(TokenType.StringLiteral, tokens[0].Type);
        Assert.Equal("line1\nline2\t\"\\\r\0a", tokens[0].Value);
    }

    [Fact]
    public void StringLiteral_Unterminated_ReportsError()
    {
        var result = TokenizeFull("\"unterminated string");

        Assert.True(result.HasErrors);
        Assert.Single(result.Errors);
        Assert.Equal("Unterminated string literal", result.Errors[0].Message);
    }

    [Fact]
    public void SingleLineComment_Ignored()
    {
        var tokens = Tokenize("// this is a comment\n42");

        Assert.Equal(TokenType.NumberLiteral, tokens[0].Type);
        Assert.Equal("42", tokens[0].Value);
    }

    [Fact]
    public void MultiLineComment_Ignored()
    {
        var tokens = Tokenize("/* multi\nline\ncomment */ 42");

        Assert.Equal(TokenType.NumberLiteral, tokens[0].Type);
        Assert.Equal("42", tokens[0].Value);
    }

    [Fact]
    public void MultiLineComment_Unterminated_ReportsError()
    {
        var result = TokenizeFull("/* unterminated comment");

        Assert.True(result.HasErrors);
        Assert.Single(result.Errors);
        Assert.Equal("Unterminated comment", result.Errors[0].Message);
    }

    [Fact]
    public void NumberLiteral_MultipleDots_ReportsError()
    {
        var result = TokenizeFull("1.2.3");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Errors, e => e.Message == "Invalid number: multiple dots.");
    }

    [Fact]
    public void UnknownCharacter_ReportsError()
    {
        var result = TokenizeFull("@");

        Assert.True(result.HasErrors);
        Assert.Single(result.Errors);
        Assert.Equal("Unexpected character: '@'", result.Errors[0].Message);
    }

    [Fact]
    public void Token_Position_CorrectLineAndColumn()
    {
        var tokens = Tokenize("int x");

        Assert.Equal(1, tokens[0].Start.Line);
        Assert.Equal(1, tokens[0].Start.Column);
        Assert.Equal(0, tokens[0].Start.Offset);

        Assert.Equal(1, tokens[1].End.Line);
        Assert.Equal(5, tokens[1].End.Column);
        Assert.Equal(4, tokens[1].End.Offset);
    }

    [Fact]
    public void MultiLineSource_Position_TrackedCorrectly()
    {
        var source = "first\nsecond\n third";
        var tokens = Tokenize(source);

        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("first", tokens[0].Value);
        Assert.Equal(1, tokens[0].Start.Line);
        Assert.Equal(1, tokens[0].Start.Column);

        Assert.Equal(TokenType.Identifier, tokens[1].Type);
        Assert.Equal("second", tokens[1].Value);
        Assert.Equal(2, tokens[1].Start.Line);
        Assert.Equal(1, tokens[1].Start.Column);

        Assert.Equal(TokenType.Identifier, tokens[2].Type);
        Assert.Equal("third", tokens[2].Value);
        Assert.Equal(3, tokens[2].Start.Line);
        Assert.Equal(2, tokens[2].Start.Column);
    }

    [Fact]
    public void StringLiteral_EscapedQuoteAtEof_ReportsError()
    {
        var result = TokenizeFull("\"escaped quote at eof\\\"");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Errors, e => e.Message == "Unterminated string literal");
    }

    [Fact]
    public void HyphenIdentifier_Vs_NegativeNumber()
    {
        var tokens = Tokenize("-abc -123");

        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("-abc", tokens[0].Value);

        Assert.Equal(TokenType.NumberLiteral, tokens[1].Type);
        Assert.Equal("-123", tokens[1].Value);
    }

    [Fact]
    public void Lexer_CanBeTokenizedMultipleTimes()
    {
        var lexer = new Lexer("test 123");
        var result1 = lexer.Tokenize();
        var result2 = lexer.Tokenize();

        Assert.Equal(result1.Tokens.Count, result2.Tokens.Count);
        Assert.Equal(result1.Tokens[0].Value, result2.Tokens[0].Value);
        Assert.Equal(result1.Tokens[1].Value, result2.Tokens[1].Value);
    }

    [Fact]
    public void ComplexSource_TokenizesEntireStructureCorrectly()
    {
        var source = """
            body {
                color: #ff00aa;
                font-size: 14.5;
                title: "Welcome";
            }
            """;

        var result = TokenizeFull(source);

        Assert.False(result.HasErrors);
        var types = result.Tokens.Select(t => t.Type).ToArray();
        var expectedTypes = new[]
        {
            TokenType.Identifier,    // body
            TokenType.LBrace,        // {
            TokenType.Identifier,    // color
            TokenType.Colon,         // :
            TokenType.ColorLiteral,  // #ff00aa
            TokenType.Semicolon,     // ;
            TokenType.Identifier,    // font-size
            TokenType.Colon,         // :
            TokenType.NumberLiteral, // 14.5
            TokenType.Semicolon,     // ;
            TokenType.Identifier,    // title
            TokenType.Colon,         // :
            TokenType.StringLiteral, // "Welcome"
            TokenType.Semicolon,     // ;
            TokenType.RBrace,        // }
            TokenType.Eof
        };

        Assert.Equal(expectedTypes, types);
    }
}