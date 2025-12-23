using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Values;
using Shouldly;

namespace CppInterpreter.Test.CppParser.Stage3;

[TestClass]
public class ParseLiteralTest
{

    [TestMethod]
    public void ParseIntLiteral()
    {
        //Arrange
        var astInt = new AstLiteral(5);
        var scope = new Scope<ICppValueBase>();
        
        //Act
        var expression = Stage3Parser.ParseLiteral(astInt);
        var result = expression(scope);

        //Assert
        result.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(5);
    }
    
}