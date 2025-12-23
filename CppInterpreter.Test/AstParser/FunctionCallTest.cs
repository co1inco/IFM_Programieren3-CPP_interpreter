using CppInterpreter.Ast;
using CppInterpreter.Test.Helper;
using Shouldly;

namespace CppInterpreter.Test.AstParser;

using static CppInterpreter.Test.Helper.ParserHelper;


[TestClass]
public class FunctionCallTest
{


    [TestMethod]
    public void ParseFunctionCall_NoParams()
    {
        //Arrange
        var tree = GetTree("test()", t => t.expression());
        
        //Act
        var ast = Ast.AstParser.ParseExpression(tree);

        //Assert
        var functionCall = ast.Value.ShouldBeOfType<AstFunctionCall>();
        functionCall.Function.Value.ShouldBeOfType<AstAtom>().Value.ShouldBe("test");
        functionCall.Arguments.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void ParseFunctionCall_WithParams()
    {
        //Arrange
        var tree = GetTree("test(foo, 5)", t => t.expression());
        
        //Act
        var ast = Ast.AstParser.ParseExpression(tree);

        //Assert
        var functionCall = ast.Value.ShouldBeOfType<AstFunctionCall>();
        functionCall.Function.Value.ShouldBeOfType<AstAtom>().Value.ShouldBe("test");
        functionCall.Arguments.ShouldHaveCount(2);
        functionCall.Arguments[0].Value.ShouldBeOfType<AstAtom>().Value.ShouldBe("foo");
        functionCall.Arguments[1].Value
            .ShouldBeOfType<AstLiteral>().Value
            .ShouldBeOfType<int>()
            .ShouldBe(5);
    }
}