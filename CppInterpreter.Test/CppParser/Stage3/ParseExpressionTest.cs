using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Values;
using Shouldly;
using static CppInterpreter.Ast.GeneratedAstTreeBuilder;

namespace CppInterpreter.Test.CppParser.Stage3;

[TestClass]
public class ParseExpressionTest
{


    [TestMethod]
    public void BinOperation()
    {
        //Arrange
        var ast = AstBinOp(
             AstLiteral(5),
             AstLiteral(6),
            AstBinOpOperator.Arithmetic.Add
        );
        var scope = new Scope<ICppValue>();
        
        //Act
        var expr = Stage3Parser.ParseExpression(ast, scope);
        var result = expr.Eval(scope);
        
        //Assert
        result.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(11);
    }
}