using Antlr4.Runtime;
using CppInterpreter.Test.Helper;
using Language;
using Shouldly;

namespace CppInterpreter.Test.Antlr;

[TestClass]
public class VarDeclaration
{

    [TestMethod]
    public void SimpleDeclaration()
    {
        //Arrange
        
        //Act
        var decl = GetTree("int test");
        
        //Assert
        decl.declType()?.typeidentifier()
            .ShouldNotBeNull()
            .GetText().ShouldBe("int");
        decl.varDeclIdent()
            .ShouldNotBeNull()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                ident => ident.IDENTIFIER().ShouldBeText("test"),
                ident => ident.pointer().ShouldBeNull()
            );
    }

    [TestMethod]
    [DataRow("int* test")]
    [DataRow("int *test")]
    [DataRow("int * test")]
    public void PointerDeclaration(string text)
    {
        //Arrange
        
        //Act
        var decl = GetTree(text);
        
        //Assert
        decl.declType()?.typeidentifier()
            .ShouldNotBeNull()
            .GetText().ShouldBe("int");
        decl.varDeclIdent()
            .ShouldNotBeNull()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                ident => ident.IDENTIFIER().ShouldBeText("test"),
                ident => ident.pointer().ShouldNotBeNull()
            );
    }
    
    [TestMethod]
    public void MultipleDeclarations()
    {
        //Arrange
        
        //Act
        var decl = GetTree("int a, b");
        
        //Assert
        decl.declType()?.typeidentifier()
            .ShouldNotBeNull()
            .GetText().ShouldBe("int");
        decl.varDeclIdent()
            .ShouldNotBeNull()
            .ShouldHaveCount(2)
            .ShouldHaveItemWith(0, ident => 
                ident.ShouldSatisfyAllConditions(
                    _ => ident.IDENTIFIER().ShouldBeText("a"),
                    _ => ident.pointer().ShouldBeNull()
                ))
            .ShouldHaveItemWith(1, ident => 
                ident.ShouldSatisfyAllConditions(
                    _ => ident.IDENTIFIER().ShouldBeText("b"),
                    _ => ident.pointer().ShouldBeNull()
                ));
    }
    
    [TestMethod]
    public void MultipleDeclarations_WithPointer()
    {
        //Arrange
        
        //Act
        var decl = GetTree("int* a, b, *c");
        
        //Assert
        decl.declType()?.typeidentifier()
            .ShouldNotBeNull()
            .GetText().ShouldBe("int");
        decl.varDeclIdent()
            .ShouldNotBeNull()
            .ShouldHaveCount(3)
            .ShouldHaveItemWith(0, ident => 
                ident.ShouldSatisfyAllConditions(
                    _ => ident.IDENTIFIER().ShouldBeText("a"),
                    _ => ident.pointer().ShouldNotBeNull()
                ))
            .ShouldHaveItemWith(1, ident => 
                ident.ShouldSatisfyAllConditions(
                    _ => ident.IDENTIFIER().ShouldBeText("b"),
                    _ => ident.pointer().ShouldBeNull()
                ))
            .ShouldHaveItemWith(2, ident => 
                ident.ShouldSatisfyAllConditions(
                    _ => ident.IDENTIFIER().ShouldBeText("c"),
                    _ => ident.pointer().ShouldNotBeNull()
                ));
    }
    
    
    [TestMethod]
    [DataRow("const int")]
    [DataRow("int const")]
    public void Type_ConstType(string text)
    {
        //Arrange
        
        //Act
        var type = ParserHelper.GetTree(text, g => g.declType());
        
        //Assert
        type.typeidentifier().ShouldNotBeNull();
        type.@const.ShouldNotBeNull();
    }
    
    [TestMethod]
    public void Type()
    {
        //Arrange
        
        //Act
        var type = ParserHelper.GetTree("int", g => g.declType());
        
        //Assert
        type.typeidentifier().ShouldNotBeNull();
        type.@const.ShouldBeNull();
    }
    

    private GrammarParser.VarDeclContext GetTree(string text) =>
        ParserHelper.GetTree(text, x => x.varDecl());

}