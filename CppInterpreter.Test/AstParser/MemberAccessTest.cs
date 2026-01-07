using CppInterpreter.Ast;
using CppInterpreter.Test.Helper;
using Shouldly;
using static CppInterpreter.Test.Helper.ParserHelper;

namespace CppInterpreter.Test.AstParser;

[TestClass]
public class MemberAccessTest
{

    [TestMethod]
    public void ParseMemberAccess()
    {
        //Arrange
        var tree = GetTree("test.member", t => t.expression());
        
        //Act
        var ast = Ast.AstParser.ParseExpression(tree);

        //Assert
        ast.Value.ShouldBeOfType<AstMemberAccess>().ShouldSatisfyAllConditions(
            x => x.Member.Value.ShouldBe("member"),
            x => x.Instance.Value.ShouldBeOfType<AstAtom>().Value.ShouldBe("test")
        );
    }
    
}