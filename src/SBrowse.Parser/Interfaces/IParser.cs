using SBrowse.Parser.ParserResults;

namespace SBrowse.Parser.Interfaces;

public interface IParser
{
    ParserResult Parse();
}