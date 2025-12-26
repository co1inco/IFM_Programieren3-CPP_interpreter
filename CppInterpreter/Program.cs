// See https://aka.ms/new-console-template for more information

using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;
using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using Language;

Console.WriteLine("Hello, World!");


// var a = new CppInt32Value(1);
// var b = new CppInt32Value(2);
// var c = a.InvokeMemberFunc("operator+", b);
//
// Console.WriteLine($"{a} + {b} = {c}");

// var scope = new CppStage1Scope()
// {
//     Values = new Scope<ICppValueBase>(),
//     Types = new Scope<ICppType>()
// };
//
// scope.Values.TryBindSymbol("test", new CppInt32Value(0));

var typeScope = new Scope<ICppType>();
typeScope.TryBindSymbol("int", CppTypes.Int32);
typeScope.TryBindSymbol("long", CppTypes.Int64);

var stage1Scope = Stage1Parser.CreateBaseScope();
var stage2Scope = Stage2Parser.CreateBaseScope();
var scope = new Scope<ICppValueBase>(stage2Scope);

while (true)
{
    var ast = ReadUserInput();
    if (ast is null)
        break;
    var s1 = Stage1Parser.ParseProgram([ ast ], stage1Scope);
    var s2 = Stage2Parser.ParseProgram(s1, stage2Scope);

    try
    {
        var stmt = Stage3Parser.ParseProgram(s2, scope);

        // Console.WriteLine($"<<< {stmt.Eval(scope)?.StringRep() ?? "<void>"}");
    }
    catch (NotImplementedException)
    {
        throw;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Eval failed: {ex.Message}");
    }
     
    // var cpp = CppInterpreter.CppParser.CppParser.ParseExpression(ast, scope);
    //
    // Console.WriteLine(cpp.Evaluate().StringRep());
}

AstStatement? ReadUserInput()
{
    Console.Write(">>> ");

    var line = "";
    while (true)
    {
        line += Console.ReadLine();

        if (line == "quit")
            return null;
        
        if (string.IsNullOrWhiteSpace(line))
            continue;
        
        var lexer = new GrammarLexer(CharStreams.fromString(line));
        var aParser = new GrammarParser(new CommonTokenStream(lexer));
        aParser.RemoveErrorListeners();
        aParser.AddErrorListener(new AntlrErrorListener());
        
        try 
        {
            return AstParser.ParseStatement(aParser.statement());
        } catch (EndOfFileException) {
            Console.Write("... ");
        }
    }
    
}


class AntlrErrorListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        if (offendingSymbol.Text == "<EOF>")
            throw new EndOfFileException();
        base.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
    }
}

public class EndOfFileException : Exception
{
    
}