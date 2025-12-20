using Shouldly;

namespace CppInterpreter.Test.AstParser;

using static CppInterpreter.Test.Helper.ParserHelper;

[TestClass]
public class AstAssignmentTest
{


    [TestMethod]
    public void ParseAssignment()
    {
        //Arrange
        var tree = GetTree("abc = 123", t => t.assignment());

        //Act
        var assignment = Ast.AstParser.ParseAssignment(tree);
        
        //Assert
        assignment.Target.Value.ShouldBe("abc");
    }
    
}