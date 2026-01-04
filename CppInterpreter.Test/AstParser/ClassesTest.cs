using CppInterpreter.Ast;
using CppInterpreter.Test.Helper;
using Shouldly;
using static CppInterpreter.Test.Helper.ParserHelper;

namespace CppInterpreter.Test.AstParser;

[TestClass]
public class ClassesTest
{


    [TestMethod]
    public void SimpleClassDefinition()
    {
        //Arrange
        var tree = GetTree("class TestClass {};", t => t.@class());

        //Act
        var ast = Ast.AstParser.ParseCompoundTypeDefinition(tree);
        
        //Assert
        ast.Ident.Value.ShouldBe("TestClass");
        ast.BaseTypes.ShouldBeEmpty();
        ast.Functions.ShouldBeEmpty();
        ast.Variables.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void ClassDefinition_WithBaseClass()
    {
        //Arrange
        var tree = GetTree("class TestClass : BaseClass1 {};", t => t.@class());

        //Act
        var ast = Ast.AstParser.ParseCompoundTypeDefinition(tree);
        
        //Assert
        ast.Ident.Value.ShouldBe("TestClass");
        ast.BaseTypes.ShouldHaveCount(1);
        ast.BaseTypes[0].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Private),
            x => x.Member.Value.ShouldBe("BaseClass1")
        );
        ast.Functions.ShouldBeEmpty();
        ast.Variables.ShouldBeEmpty();
    }
 
    [TestMethod]
    public void ClassDefinition_WithBaseClasses()
    {
        //Arrange
        var tree = GetTree("class TestClass : BaseClass1, public BaseClass2, private BaseClass3 {};", t => t.@class());

        //Act
        var ast = Ast.AstParser.ParseCompoundTypeDefinition(tree);
        
        //Assert
        ast.Ident.Value.ShouldBe("TestClass");
        ast.BaseTypes.ShouldHaveCount(3);
        ast.BaseTypes[0].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Private),
            x => x.Member.Value.ShouldBe("BaseClass1")
        );
        ast.BaseTypes[1].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Public),
            x => x.Member.Value.ShouldBe("BaseClass2")
        );
        ast.BaseTypes[2].ShouldSatisfyAllConditions(
            x => x.Visibility.ShouldBe(AstVisibility.Private),
            x => x.Member.Value.ShouldBe("BaseClass3")
        );
        
        ast.Functions.ShouldBeEmpty();
        ast.Variables.ShouldBeEmpty();
    }

    
}