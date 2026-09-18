using SBrowse.Lexer.LexerResults;

namespace SBrowse.Lexer.Interfaces;

public interface ILexer
{
    LexerResult Tokenize();
}