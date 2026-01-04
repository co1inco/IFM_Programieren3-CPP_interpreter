using CppInterpreter.Ast;
using CppInterpreter.Test.Helper;
using Shouldly;

namespace CppInterpreter.Test.AstParser;
using static CppInterpreter.Test.Helper.ParserHelper;

[TestClass]
public class SuffixOperationTest
{


    [TestMethod]
    public void AntlrParseSuffixOperation()
    {
        //Arrange

        //Act
        var ctx = GetTree("test++", t => t.expression());
        
        //Assert
        ctx.suffix.Text.ShouldBe("++");
        ctx.expression().ShouldHaveCount(1);
    }
    
    [TestMethod]
    public void ParseSuffixOperation()
    {
        //Arrange
        var ctx = GetTree("test++", t => t.expression());
        
        //Act
        var ast = Ast.AstParser.ParseExpression(ctx);

        //Assert
        ast.Value.ShouldBeOfType<AstSuffix>().ShouldSatisfyAllConditions(
            s => s.Operator.Value.ShouldBe("++"),
            s => s.Expression.Value.ShouldBeOfType<AstAtom>().Value.ShouldBe("test")
        );
    }
}