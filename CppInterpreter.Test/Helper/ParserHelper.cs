using Antlr4.Runtime;
using Language;

namespace CppInterpreter.Test.Helper;

public static class ParserHelper
{

    public static T GetTree<T>(string text, Func<GrammarParser, T> selector)
    {
        var lexer = new GrammarLexer(CharStreams.fromString(text));
        var parser = new GrammarParser(new CommonTokenStream(lexer));
        
        parser.RemoveErrorListeners();
        var el = new ErrorListener();
        parser.AddErrorListener(el);
        
        if (!string.IsNullOrEmpty(el.Error))
            throw new Exception(el.Error);
        
        return selector(parser);
    }
    
    public class ErrorListener : IAntlrErrorListener<IToken>
    {
        public string? Error { get; set; }
        
        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            Error = $"{line}:{charPositionInLine} {msg}";
        }
    }
    
}