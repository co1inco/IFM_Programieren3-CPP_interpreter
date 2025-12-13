using CppInterpreter.Test.Helper;
using Shouldly;

using static CppInterpreter.Test.Helper.ParserHelper;

namespace CppInterpreter.Test.AstParser;

[TestClass]
public class TypeDefinitionTest
{

    [TestMethod]
    [DataRow("int")]
    [DataRow("string")]
    [DataRow("adsad")]
    public void ParseTypeUsage(string type)
    {
        //Arrange
         var tree = ParserHelper.GetTree(type, t => t.typeIdentifierUsage());

        //Act
        var ast = CppInterpreter.AstParser.ParseTypeUsage(tree);
        
        //Assert
        ast.Ident.ShouldBe(type);
        ast.IsReference.ShouldBeFalse();
    }
    
    
    [TestMethod]
    [DataRow("int")]
    [DataRow("string")]
    [DataRow("adsad")]
    public void ParseTypeUsage_Reference(string type)
    {
        //Arrange
        var tree = ParserHelper.GetTree($"{type}&", t => t.typeIdentifierUsage());

        //Act
        var ast = CppInterpreter.AstParser.ParseTypeUsage(tree);
        
        //Assert
        ast.Ident.ShouldBe(type);
        ast.IsReference.ShouldBeTrue();
    }

    [TestMethod]
    public void ParseTypeUsage_Void()
    {
        //Arrange
        var tree = ParserHelper.GetTree($"void", t => t.typeIdentifierUsage());
        
        //Act Assert
        Should.Throw<ParserException>(() => CppInterpreter.AstParser.ParseTypeUsage(tree));
    }


    [TestMethod]
    public void ParseVarDefinition()
    {
        //Arrange
        var tree = GetTree("int test", t => t.variableDefinition());

        //Act
        var ast = CppInterpreter.AstParser.ParseVarDefinition(tree);
        
        //Assert
        ast.Type.Ident.ShouldBe("int");
        ast.Value.Value.ShouldBe("test");
    }
}