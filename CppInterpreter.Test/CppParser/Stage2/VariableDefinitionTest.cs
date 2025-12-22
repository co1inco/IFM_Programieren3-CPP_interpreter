using System.CodeDom;
using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using Shouldly;

namespace CppInterpreter.Test.CppParser.Stage2;

[TestClass]
public class VariableDefinitionTest
{

    [TestMethod]
    public void ParseVariableDefinition()
    {
        //Arrange
        var ast = new AstVarDefinition(
            new AstTypeIdentifier("int", false), 
            new AstIdentifier("test"),
            null);

        var typeScope = new Scope<ICppType>();
        typeScope.TryBindSymbol("int", new CppInt32Type());
        
        var scope = new Scope<ICppValueBase>();
        
        //Act
        var result = Stage2Parser.ParseVarDefinition(ast, scope, typeScope);
        
        //Assert
        result.Initializer.ShouldBeNull();

        scope.TryGetSymbol("test", out var test).ShouldBeTrue();
        test.ShouldBeOfType<CppInt32Value>();
    }
    
    [TestMethod]
    public void ParseVariableDefinition_WithAssignment()
    {
        //Arrange

        var initializer = new AstLiteral(5);
        var ast = new AstVarDefinition(
            new AstTypeIdentifier("int", false), 
            new AstIdentifier("test"),
            new AstExpression(initializer));

        var typeScope = new Scope<ICppType>();
        typeScope.TryBindSymbol("int", new CppInt32Type());
        
        var scope = new Scope<ICppValueBase>();
        
        //Act
        var result = Stage2Parser.ParseVarDefinition(ast, scope, typeScope);
        
        //Assert
        result.Initializer.ShouldNotBeNull();

        scope.TryGetSymbol("test", out var test).ShouldBeTrue();
        test.ShouldBeOfType<CppInt32Value>();
    }
    
    
    [TestMethod]
    public void ParseVariableDefinition_UnknownType()
    {
        //Arrange
        var ast = new AstVarDefinition(
            new AstTypeIdentifier("dummy", false), 
            new AstIdentifier("test"),
            null);

        var typeScope = new Scope<ICppType>();
        typeScope.TryBindSymbol("int", new CppInt32Type());
        
        var scope = new Scope<ICppValueBase>();
        
        //Act
        
        //Assert
        Should.Throw<Exception>(() => Stage2Parser.ParseVarDefinition(ast, scope, typeScope))
            .Message.ShouldBe($"Unknown type 'dummy'");
    }
}