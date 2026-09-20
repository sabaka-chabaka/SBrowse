using SBrowse.Lexer;
using SBrowse.Lexer.Tokens;
using SBrowse.Parser.AST;
using SBrowse.Parser.ParserResults;
using Xunit;

namespace SBrowse.Parser.Tests;

public class ParserTests
{
    private static ParserResult Parse(string source)
    {
        var lexerResult = new Lexer.Lexer(source).Tokenize();
        return new Parser(lexerResult.Tokens).Parse();
    }

    [Fact]
    public void EmptyInput_ReturnsNoStatementsAndNoErrors()
    {
        var result = Parse("");

        Assert.False(result.HasErrors);
        Assert.Empty(result.Stmts);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void PropertyStatement_WithNumberLiteral()
    {
        var result = Parse("width: 100;");

        Assert.False(result.HasErrors);
        var stmt = Assert.Single(result.Stmts);
        var prop = Assert.IsType<PropertyStatement>(stmt);

        Assert.Equal("width", prop.Identifier);
        var num = Assert.IsType<NumberLit>(prop.Value);
        Assert.Equal(100, num.Value);

        Assert.Equal(1, prop.Span.Start.Line);
        Assert.Equal(1, prop.Span.Start.Column);
    }

    [Fact]
    public void PropertyStatement_WithStringLiteral()
    {
        var result = Parse("title: \"Hello, world!\";");

        Assert.False(result.HasErrors);
        var stmt = Assert.Single(result.Stmts);
        var prop = Assert.IsType<PropertyStatement>(stmt);

        Assert.Equal("title", prop.Identifier);
        var str = Assert.IsType<StringLit>(prop.Value);
        Assert.Equal("Hello, world!", str.Value);
    }

    [Fact]
    public void PropertyStatement_WithColorLiteral()
    {
        var result = Parse("background-color: #ff00aa;");

        Assert.False(result.HasErrors);
        var stmt = Assert.Single(result.Stmts);
        var prop = Assert.IsType<PropertyStatement>(stmt);

        Assert.Equal("background-color", prop.Identifier);
        var color = Assert.IsType<ColorLit>(prop.Value);
        Assert.Equal("#ff00aa", color.Value);
    }

    [Fact]
    public void PropertyStatement_WithIdentifierExpr()
    {
        var result = Parse("display: block;");

        Assert.False(result.HasErrors);
        var stmt = Assert.Single(result.Stmts);
        var prop = Assert.IsType<PropertyStatement>(stmt);

        Assert.Equal("display", prop.Identifier);
        var id = Assert.IsType<IdentifierExpr>(prop.Value);
        Assert.Equal("block", id.Name);
    }

    [Fact]
    public void MultiplePropertyStatements()
    {
        var source = """
            width: 100;
            height: 200;
            color: #000;
            """;
        var result = Parse(source);

        Assert.False(result.HasErrors);
        Assert.Equal(3, result.Stmts.Count);
        Assert.Equal("width", Assert.IsType<PropertyStatement>(result.Stmts[0]).Identifier);
        Assert.Equal("height", Assert.IsType<PropertyStatement>(result.Stmts[1]).Identifier);
        Assert.Equal("color", Assert.IsType<PropertyStatement>(result.Stmts[2]).Identifier);
    }

    [Fact]
    public void BlockStatement_Empty()
    {
        var result = Parse("body {}");

        Assert.False(result.HasErrors);
        var stmt = Assert.Single(result.Stmts);
        var block = Assert.IsType<BlockStatement>(stmt);

        Assert.Equal("body", block.Identifier);
        Assert.Empty(block.Statements);
    }

    [Fact]
    public void BlockStatement_WithProperties()
    {
        var source = """
            body {
                color: #ff00aa;
                font-size: 14;
                title: "Welcome";
            }
            """;
        var result = Parse(source);

        Assert.False(result.HasErrors);
        var stmt = Assert.Single(result.Stmts);
        var block = Assert.IsType<BlockStatement>(stmt);

        Assert.Equal("body", block.Identifier);
        Assert.Equal(3, block.Statements.Count);

        var p1 = Assert.IsType<PropertyStatement>(block.Statements[0]);
        Assert.Equal("color", p1.Identifier);
        Assert.Equal("#ff00aa", Assert.IsType<ColorLit>(p1.Value).Value);

        var p2 = Assert.IsType<PropertyStatement>(block.Statements[1]);
        Assert.Equal("font-size", p2.Identifier);
        Assert.Equal(14, Assert.IsType<NumberLit>(p2.Value).Value);

        var p3 = Assert.IsType<PropertyStatement>(block.Statements[2]);
        Assert.Equal("title", p3.Identifier);
        Assert.Equal("Welcome", Assert.IsType<StringLit>(p3.Value).Value);
    }

    [Fact]
    public void BlockStatement_Nested()
    {
        var source = """
            main {
                header {
                    color: #fff;
                }
                footer {
                    title: "Footer";
                }
            }
            """;
        var result = Parse(source);

        Assert.False(result.HasErrors);
        var main = Assert.IsType<BlockStatement>(Assert.Single(result.Stmts));
        Assert.Equal("main", main.Identifier);
        Assert.Equal(2, main.Statements.Count);

        var header = Assert.IsType<BlockStatement>(main.Statements[0]);
        Assert.Equal("header", header.Identifier);
        Assert.Single(header.Statements);

        var footer = Assert.IsType<BlockStatement>(main.Statements[1]);
        Assert.Equal("footer", footer.Identifier);
        Assert.Single(footer.Statements);
    }

    [Fact]
    public void Spans_AreSetAccurately()
    {
        var source = "body { color: #fff; }";
        var result = Parse(source);

        Assert.False(result.HasErrors);
        var block = Assert.IsType<BlockStatement>(Assert.Single(result.Stmts));
        Assert.Equal(1, block.Span.Start.Line);
        Assert.Equal(1, block.Span.Start.Column);
        Assert.Equal(0, block.Span.Start.Offset);

        Assert.Equal(1, block.Span.End.Line);
        Assert.Equal(21, block.Span.End.Column);
        Assert.Equal(20, block.Span.End.Offset);

        var prop = Assert.IsType<PropertyStatement>(Assert.Single(block.Statements));
        Assert.Equal(8, prop.Span.Start.Column);
        Assert.Equal(19, prop.Span.End.Column);

        var val = Assert.IsType<ColorLit>(prop.Value);
        Assert.Equal(15, val.Span.Start.Column);
        Assert.Equal(18, val.Span.End.Column);
    }

    [Fact]
    public void Error_UnexpectedTokenAtStatementStart()
    {
        var result = Parse("123;");

        Assert.True(result.HasErrors);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Error_MissingColonInProperty()
    {
        var result = Parse("width 100;");

        Assert.True(result.HasErrors);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Error_MissingExpressionInProperty()
    {
        var result = Parse("width: ;");

        Assert.True(result.HasErrors);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Error_MissingSemicolonInProperty()
    {
        var result = Parse("width: 100");

        Assert.True(result.HasErrors);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Error_UnclosedBlock()
    {
        var result = Parse("body { color: #fff;");

        Assert.True(result.HasErrors);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void ErrorRecovery_ParsesValidStatementsAfterError()
    {
        var source = """
            invalid statement here;
            validProp: 42;
            anotherInvalid: ;
            finalProp: "works";
            """;
        var result = Parse(source);

        Assert.True(result.HasErrors);
        Assert.Equal(2, result.Stmts.Count);

        var p1 = Assert.IsType<PropertyStatement>(result.Stmts[0]);
        Assert.Equal("validProp", p1.Identifier);

        var p2 = Assert.IsType<PropertyStatement>(result.Stmts[1]);
        Assert.Equal("finalProp", p2.Identifier);
    }

    [Fact]
    public void Parser_CanBeInvokedMultipleTimes()
    {
        var lexerResult = new Lexer.Lexer("width: 100;").Tokenize();
        var parser = new Parser(lexerResult.Tokens);

        var res1 = parser.Parse();
        var res2 = parser.Parse();

        Assert.Equal(res1.Stmts.Count, res2.Stmts.Count);
        Assert.Equal(res1.Errors.Count, res2.Errors.Count);
    }
}
