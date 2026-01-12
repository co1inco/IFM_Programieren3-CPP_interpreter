using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Values;
using Shouldly;
using static CppInterpreter.Ast.GeneratedAstTreeBuilder;

namespace CppInterpreter.Test.Interpreter;

[TestClass]
public class AssignmentTest
{

    [TestMethod]
    public void AssignIncorrectType()
    {
        //Arrange
        var ast = AstAssignment(AstIdentifier("test"), AstLiteral(5));

        var scope = new Scope<ICppValue>();
        scope.TryBindSymbol("test", new CppStringValue("Hello"));
        
        //Act
        
        //Assert
        Should.Throw<ParserException>(() => Stage3ExpressionParser.ParseAssignment(ast, scope));
    }
}