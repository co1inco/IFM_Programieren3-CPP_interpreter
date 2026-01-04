using Antlr4.Runtime;
using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using Language;
using Shouldly;

namespace CppInterpreter.Test.AstParser;

[TestClass]
public class ExamplesTest
{
    public static string[] PositiveFiles { get; private set; }
    public static string[] NegativeFiles { get; private set; }
    
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {

        var posFiles = Directory.GetFiles("Examples/tests/pos")
            .Where(x => x.EndsWith(".cpp"))
            .ToArray();
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
    public void AstParser_Positive(string filename)
    {
        //Arrange
        var lexer = new GrammarLexer(CharStreams.fromPath(filename));
        var parser = new GrammarParser(new CommonTokenStream(lexer));
        parser.FailOnParserError();

        var stdOut = new StringWriter();
        
        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = Stage2Parser.CreateBaseScope(stdOut);
        
        //Act
        var context = parser.program();
        
        var ast = Ast.AstParser.ParseProgram(context);

        
        //Assert
        ast.ShouldNotBeNull();
    }
}