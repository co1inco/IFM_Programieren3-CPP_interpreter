using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Values;
using Shouldly;
using static CppInterpreter.Ast.GeneratedAstTreeBuilder;

namespace CppInterpreter.Test.CppParser.Stage3;

[TestClass]
public class ParseAtomTest
{

    [TestMethod]
    public void GetAtom()
    {
        //Arrange
        var ast = AstAtom("test");
        var scope = new Scope<ICppValue>();

        var value = new CppInt32Value(42);

        scope.TryBindSymbol("test", value);

        //Act
        var expr = Stage3Parser.ParseAtom(ast, scope);
        var result = expr.Eval(scope);
        
        //Assert
        result.ShouldBe(value);
    }
    
    [TestMethod]
    public void GetAtom_UnknownVariable()
    {
        //Arrange
        var ast = AstAtom("test");
        var scope = new Scope<ICppValue>();

        var value = new CppInt32Value(42);

        scope.TryBindSymbol("dummy", value);

        //Act //Assert
        Should.Throw<ParserException>(() => Stage3Parser.ParseAtom(ast, scope))
            .BaseMessage.ShouldBe("Undefined value");
    }
}