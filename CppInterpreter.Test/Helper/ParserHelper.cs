using Antlr4.Runtime;
using Language;

namespace CppInterpreter.Test.Helper;

public static class ParserHelper
{

    public static T GetTree<T>(string text, Func<GrammarParser, T> selector)
    {
        var lexer = new GrammarLexer(CharStreams.fromString(text));
        var parser = new GrammarParser(new CommonTokenStream(lexer));
        return selector(parser);
    }
    
    
    
}