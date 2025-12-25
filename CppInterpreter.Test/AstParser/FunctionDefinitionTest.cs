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
        ast.Symbol.Ident.Value.ShouldBe("test");
        ast.Symbol.ReturnType.Ident.ShouldBe("void");
        ast.Symbol.Arguments.ShouldBeEmpty();
        ast.Symbol.Body.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void FunctionDefinition_Arguments()
    {
        //Arrange
        var tree = GetTree("void test(int foo, bool bar) {}", t => t.functionDefinition());

        //Act
        var ast = Ast.AstParser.ParseFunctionDefinition(tree);

        //Assert
        ast.Symbol.Ident.Value.ShouldBe("test");
        ast.Symbol.ReturnType.Ident.ShouldBe("void");
        ast.Symbol.Body.ShouldBeEmpty();
        
        ast.Symbol.Arguments.ShouldHaveCount(2);
        ast.Symbol.Arguments[0].Type.Ident.ShouldBe("int");
        ast.Symbol.Arguments[0].Ident.Value.ShouldBe("foo");
        ast.Symbol.Arguments[1].Type.Ident.ShouldBe("bool");
        ast.Symbol.Arguments[1].Ident.Value.ShouldBe("bar");
    }
        
    [TestMethod]
    public void FunctionDefinition_ReturnType()
    {
        //Arrange
        var tree = GetTree("int test() {}", t => t.functionDefinition());

        //Act
        var ast = Ast.AstParser.ParseFunctionDefinition(tree);

        //Assert
        ast.Symbol.Ident.Value.ShouldBe("test");
        ast.Symbol.ReturnType.Ident.ShouldBe("int");
        ast.Symbol.Body.ShouldBeEmpty();
        ast.Symbol.Arguments.ShouldBeEmpty();
    }
    
}