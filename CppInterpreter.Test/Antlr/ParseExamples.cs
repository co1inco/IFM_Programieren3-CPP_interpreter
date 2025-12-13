using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using Language;
using Shouldly;

namespace CppInterpreter.Test.Antlr;

[TestClass]
public class ParseExamples
{

    public static string[] PositiveFiles { get; private set; }
    public static string[] NegativeFiles { get; private set; }
    
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {

        var posFiles = Directory.GetFiles("Examples/tests/pos");
        if (posFiles.Length == 0)
            throw new Exception("No positive examples found");
        
        var negFiles = Directory.GetFiles("Examples/tests/neg");
        if (negFiles.Length == 0)
            throw new Exception("No negative examples found");
        
        PositiveFiles = posFiles;
        NegativeFiles = negFiles;
    }

    [TestMethod]
    [DynamicData(nameof(PositiveFiles))]
    public void Positive(string filename)
    {
        //Arrange
        var lexer = new GrammarLexer(CharStreams.fromPath(filename));
        var parser = new GrammarParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();

        var errorListener = new ErrorListener();
        parser.AddErrorListener(errorListener);
        
        //Act
        var context = parser.program();
        
        //Assert
        
        context.ShouldNotBeNull();
        context.exception.ShouldBeNull();
        errorListener.Error.ShouldBeNull();
        // context.IsEmpty.ShouldBeFalse();
    }
    
    private class ErrorListener : IAntlrErrorListener<IToken>
    {
        public string? Error { get; set; }
        
        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            Error = $"{line}:{charPositionInLine} {msg}";
        }
    }
    
    
    // No tests for antlr parser    
    // [TestMethod]
    // [DynamicData(nameof(NegativeFiles))]
    // public void Negative(string filename)
    // {
    //     //Arrange
    //     var lexer = new GrammarLexer(CharStreams.fromPath(filename));
    //     var parser = new GrammarParser(new CommonTokenStream(lexer));
    //     
    //     //Act
    //     var context = parser.program();
    //     
    //     //Assert
    //     
    //     context.exception.ShouldNotBeNull();
    //     // context.IsEmpty.ShouldBeFalse();
    // }
}