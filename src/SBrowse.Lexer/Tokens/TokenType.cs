namespace SBrowse.Lexer.Tokens;

public enum TokenType
{
    Identifier,
    
    LBrace,
    RBrace,
    Colon,
    Semicolon,
    Comma,
    
    ColorLiteral,
    StringLiteral,
    
    NumberLiteral,
    
    Eof
}