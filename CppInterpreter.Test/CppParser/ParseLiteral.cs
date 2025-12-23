using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Values;
using Shouldly;

namespace CppInterpreter.Test.CppParser;

[TestClass]
public class ParseLiteral
{

    [TestMethod]
    public void ParseIntLiteral()
    {
        //Arrange
        var astInt = new AstLiteral(5);

        //Act
        var cppValue = CppInterpreter.CppParser.CppParser.ParseLiteral(astInt)
            .Evaluate();

        //Assert
        cppValue.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(5);
    }
    
}