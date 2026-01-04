using CppInterpreter.Ast;
using CppInterpreter.Test.Helper;
using Shouldly;
using static CppInterpreter.Test.Helper.ParserHelper;

namespace CppInterpreter.Test.AstParser;

[TestClass]
public class PrefixOperationTest
{
    [TestMethod]
    public void AntlrParsePrefixOperation()
    {
        //Arrange

        //Act
        var ctx = GetTree("++test", t => t.expression());
        
        //Assert
        ctx.unary.Text.ShouldBe("++");
        ctx.expression().ShouldHaveCount(1);
    }
    
    [TestMethod]
    public void ParsePrefixOperation()
    {
        //Arrange
        var ctx = GetTree("++test", t => t.expression());
        
        //Act
        var ast = Ast.AstParser.ParseExpression(ctx);

        //Assert
        ast.Value.ShouldBeOfType<AstUnary>().ShouldSatisfyAllConditions(
            s => s.Operator.ShouldBe(AstUnary.UnaryOperator.Increment),
            s => s.Expression.Value.ShouldBeOfType<AstAtom>().Value.ShouldBe("test")
        );
    }
}