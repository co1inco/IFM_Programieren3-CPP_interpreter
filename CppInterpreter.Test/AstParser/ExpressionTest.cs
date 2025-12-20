using CppInterpreter.Ast;
using Shouldly;
namespace CppInterpreter.Test.AstParser;

using static CppInterpreter.Test.Helper.ParserHelper;

[TestClass]
public class ExpressionTest
{

    [TestMethod]
    public void ParseLiteral()
    {
        //Arrange
        var tree = GetTree("123", t => t.expression());

        //Act
        var expression = Ast.AstParser.ParseExpression(tree);
        
        //Assert
        expression.Value.ShouldBeOfType<AstLiteral>()
            .Value.ShouldBeAssignableTo<int>()
            .ShouldBe(123);
    }
    
    [TestMethod]
    public void ParseAtom()
    {
        //Arrange
        var tree = GetTree("hello", t => t.expression());

        //Act
        var expression = Ast.AstParser.ParseExpression(tree);
        
        //Assert
        expression.Value.ShouldBeOfType<Ast.AstAtom>()
            .Value.ShouldBe("hello");
    }
    
    [TestMethod]
    public void ParseAssignment()
    {
        //Arrange
        var tree = GetTree("hello = world", t => t.expression());

        //Act
        var expression = Ast.AstParser.ParseExpression(tree);
        
        //Assert
        expression.Value.ShouldBeOfType<Ast.AstAssignment>()
            .Target.Value.ShouldBe("hello");
    }
    
    [TestMethod]
    public void ParseOperation()
    {
        //Arrange
        var tree = GetTree("5 + 6", t => t.expression());

        //Act
        var expression = Ast.AstParser.ParseExpression(tree);
        
        //Assert
        var binOp = expression.Value.ShouldBeOfType<Ast.AstBinOp>();
        binOp.Left.Value.ShouldBeAssignableTo<AstLiteral>().Value.ShouldBe(5);
        binOp.Right.Value.ShouldBeAssignableTo<AstLiteral>().Value.ShouldBe(6);
    }
    
}