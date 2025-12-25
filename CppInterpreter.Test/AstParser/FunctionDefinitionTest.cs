using CppInterpreter.Test.Helper;
using Shouldly;

namespace CppInterpreter.Test.AstParser;
using static CppInterpreter.Test.Helper.ParserHelper;

[TestClass]
public class FunctionDefinitionTest
{

    [TestMethod]
    public void FunctionDefinition_Empty()
    {
        //Arrange
        var tree = GetTree("void test() {}", t => t.functionDefinition());

        //Act
        var ast = Ast.AstParser.ParseFunctionDefinition(tree);

        //Assert
        ast.Ident.Value.ShouldBe("test");
        ast.ReturnType.Ident.ShouldBe("void");
        ast.Arguments.ShouldBeEmpty();
        ast.Body.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void FunctionDefinition_Arguments()
    {
        //Arrange
        var tree = GetTree("void test(int foo, bool bar) {}", t => t.functionDefinition());

        //Act
        var ast = Ast.AstParser.ParseFunctionDefinition(tree);

        //Assert
        ast.Ident.Value.ShouldBe("test");
        ast.ReturnType.Ident.ShouldBe("void");
        ast.Body.ShouldBeEmpty();
        
        ast.Arguments.ShouldHaveCount(2);
        ast.Arguments[0].Type.Ident.ShouldBe("int");
        ast.Arguments[0].Ident.Value.ShouldBe("foo");
        ast.Arguments[1].Type.Ident.ShouldBe("bool");
        ast.Arguments[1].Ident.Value.ShouldBe("bar");
    }
        
    [TestMethod]
    public void FunctionDefinition_ReturnType()
    {
        //Arrange
        var tree = GetTree("int test() {}", t => t.functionDefinition());

        //Act
        var ast = Ast.AstParser.ParseFunctionDefinition(tree);

        //Assert
        ast.Ident.Value.ShouldBe("test");
        ast.ReturnType.Ident.ShouldBe("int");
        ast.Body.ShouldBeEmpty();
        ast.Arguments.ShouldBeEmpty();
    }
    
}