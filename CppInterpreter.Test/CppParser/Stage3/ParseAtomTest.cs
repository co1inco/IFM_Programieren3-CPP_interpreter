using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using Shouldly;

namespace CppInterpreter.Test.CppParser.Stage3;

[TestClass]
public class ParseAtomTest
{

    [TestMethod]
    public void GetAtom()
    {
        //Arrange
        var ast = new AstAtom("test");
        var scope = new Scope<ICppValueBase>();

        var value = new CppInt32Value(42);

        scope.TryBindSymbol("test", value);

        //Act
        var expr = Stage3Parser.ParseAtom(ast);
        var result = expr(scope);
        
        //Assert
        result.ShouldBe(value);
    }
    
    [TestMethod]
    public void GetAtom_UnknownVariable()
    {
        //Arrange
        var ast = new AstAtom("test");
        var scope = new Scope<ICppValueBase>();

        var value = new CppInt32Value(42);

        scope.TryBindSymbol("dummy", value);

        //Act
        var expr = Stage3Parser.ParseAtom(ast);
        
        //Assert
        Should.Throw<Exception>(() => expr(scope));
    }
}