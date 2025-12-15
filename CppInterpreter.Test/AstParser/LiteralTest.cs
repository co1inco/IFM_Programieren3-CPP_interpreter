using Shouldly;

namespace CppInterpreter.Test.AstParser;

using static CppInterpreter.Test.Helper.ParserHelper;

[TestClass]
public class LiteralTest
{



    [TestMethod]
    public void DecimalIntLiteral()
    {
        //Arrange
        var tree = GetTree("1234", t => t.literal());

        //Act
        var literal = CppInterpreter.AstParser.ParseLiteral(tree);

        //Assert
        literal.Value.ShouldBeOfType<int>().ShouldBe(1234);   
    }
    
    [TestMethod]
    public void HexIntLiteral()
    {
        //Arrange
        var tree = GetTree("0x1234", t => t.literal());

        //Act
        var literal = CppInterpreter.AstParser.ParseLiteral(tree);

        //Assert
        literal.Value.ShouldBeOfType<int>().ShouldBe(0x1234);   
    }
    
    [TestMethod]
    public void BinIntLiteral()
    {
        //Arrange
        var tree = GetTree("0b1010", t => t.literal());

        //Act
        var literal = CppInterpreter.AstParser.ParseLiteral(tree);

        //Assert
        literal.Value.ShouldBeOfType<int>().ShouldBe(10);   
    }
    
    [TestMethod]
    public void CharLiteral()
    {
        //Arrange
        var tree = GetTree("'c'", t => t.literal());

        //Act
        var literal = CppInterpreter.AstParser.ParseLiteral(tree);

        //Assert
        literal.Value.ShouldBeOfType<char>().ShouldBe('c');   
    }
    
    [TestMethod]
    public void StringLiteral()
    {
        //Arrange
        var tree = GetTree("\"Hello world!\"", t => t.literal());

        //Act
        var literal = CppInterpreter.AstParser.ParseLiteral(tree);

        //Assert
        literal.Value.ShouldBeOfType<string>().ShouldBe("Hello world!");   
    }
}