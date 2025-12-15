using Shouldly;

namespace CppInterpreter.Test.AstParser;

using static CppInterpreter.Test.Helper.ParserHelper;

[TestClass]
public class AssignmentTest
{


    [TestMethod]
    public void ParseAssignment()
    {
        //Arrange
        var tree = GetTree("abc = 123", t => t.assignment());

        //Act
        var assignment = CppInterpreter.AstParser.ParseAssignment(tree);
        
        //Assert
        assignment.Target.Value.ShouldBe("abc");
    }
    
}