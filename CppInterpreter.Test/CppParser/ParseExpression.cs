using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using Shouldly;

namespace CppInterpreter.Test.CppParser;

[TestClass]
public class ParseExpression
{


    [TestMethod]
    public void BinOperation()
    {
        //Arrange
        var ast = new AstExpression(new AstBinOp(
            new AstExpression(new AstLiteral(5)),
            new AstExpression(new AstLiteral(6)),
            AstBinOpOperator.Arithmetic.Add
        ));
        
        //Act
        var expr = CppInterpreter.CppParser.CppParser.ParseExpression(ast, null!);
        var result = expr.Evaluate();
        
        //Assert
        result.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(11);
    }
}