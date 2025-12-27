using System.Text.RegularExpressions;
using Antlr4.Runtime;
using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Test.Helper;
using Language;
using Shouldly;

namespace CppInterpreter.Test.Interpreter;

[TestClass]
public class Examples
{
    private static readonly Regex ExpectedParser = new(@"/\* EXPECT(?: \(Zeile für Zeile\))?:(?:\r\n|\r|\n)((?:(.*?)(?:\r\n|\r|\n))*?)\*/", RegexOptions.Multiline);
    
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
    public void Positive(string filename)
    {
        var expected = GetExpectedOutput(filename);
        var output = EvaluateFile(filename);
        
        output.Replace("\r\n", "\n") .ShouldBe(expected);
    }

    [TestMethod]
    public void Check_Negative()
    {
        var filename = "Examples/tests/Check_01.cpp";
        var expected = GetExpectedOutput(filename);
        var output = EvaluateFile(filename);
        
        output.ShouldNotBe(expected);
    }
    
    [TestMethod]
    // [DataRow("P01_vars.cpp")]
    // [DataRow("P02_expr.cpp")]
    [DataRow("P05_operators.cpp")]
    public void Positive_Manual(string filename)
    {
        //Arrange
        filename = "Examples/tests/pos/" + filename;
        var lexer = new GrammarLexer(CharStreams.fromPath(filename));
        var parser = new GrammarParser(new CommonTokenStream(lexer));
        parser.FailOnParserError();

        var stdOut = new StringWriter();
        
        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = Stage2Parser.CreateBaseScope(stdOut);
        
        var expected = GetExpectedOutput(filename).Replace("\r\n", "\n");
        
        //Act
        var context = parser.program();
        
        var ast = Ast.AstParser.ParseProgram(context);
        var s1 = Stage1Parser.ParseProgram(ast, typeScope);
        var s2 = Stage2Parser.ParseProgram(s1, valueScope);
        var s3= Stage3Parser.ParseProgram(s2, valueScope);

        var _ = s3.Eval(valueScope);

        var result = valueScope.ExecuteFunction("main");
        
        //Assert

        var output = stdOut.GetStringBuilder().ToString();
        
        output.Replace("\r\n", "\n") .ShouldBe(expected);
    }

    private string EvaluateFile(string filename)
    {
        var lexer = new GrammarLexer(CharStreams.fromPath(filename));
        var parser = new GrammarParser(new CommonTokenStream(lexer));
        parser.FailOnParserError();

        var stdOut = new StringWriter();
        
        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = Stage2Parser.CreateBaseScope(stdOut);
        
        //Act
        var context = parser.program();
        
        var ast = Ast.AstParser.ParseProgram(context);
        var s1 = Stage1Parser.ParseProgram(ast, typeScope);
        var s2 = Stage2Parser.ParseProgram(s1, valueScope);
        var s3= Stage3Parser.ParseProgram(s2, valueScope);

        var _ = s3.Eval(valueScope);

        var result = valueScope.ExecuteFunction("main");
        
        //Assert

        return stdOut.GetStringBuilder().ToString().Replace("\r\n", "\n");
    }

    
    private string GetExpectedOutput(string source)
    {
        var text = File.ReadAllText(source);
        var match = ExpectedParser.Match(text);

        if (!match.Success)
            throw new Exception($"Failed to parse expected output for file: {source}");
        return match.Groups[1].Value.Replace("\r\n", "\n");
    }
}