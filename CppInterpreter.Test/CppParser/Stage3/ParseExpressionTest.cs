using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using Shouldly;

namespace CppInterpreter.Test.CppParser.Stage3;

[TestClass]
public class ParseExpressionTest
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
        var scope = new Scope<ICppValueBase>();
        
        //Act
        var expr = Stage3Parser.ParseExpression(ast);
        var result = expr(scope);
        
        //Assert
        result.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(11);
    }
}