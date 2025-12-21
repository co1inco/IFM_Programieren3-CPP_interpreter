using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using Shouldly;

namespace CppInterpreter.Test.CppParser;

[TestClass]
public class ParseAtomTest
{

    [TestMethod]
    public void GetAtom()
    {
        //Arrange
        var ast = new AstAtom("test");

        var scope = new CppStage1Scope()
        {
            Types = null!,
            Values = new Scope<ICppValueBase>()
        };

        var value = new CppInt32Value(42);

        scope.Values.TryBindSymbol("test", value);

        //Act
        var expr = CppInterpreter.CppParser.CppParser.ParseAtom(ast, scope);
        var exprValue = expr.Evaluate();

        //Assert
        exprValue.ShouldBe(value);
    }
    
    
}