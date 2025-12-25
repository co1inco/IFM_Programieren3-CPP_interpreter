using System.CodeDom;
using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using Shouldly;
using static CppInterpreter.Ast.GeneratedAstTreeBuilder;

namespace CppInterpreter.Test.CppParser.Stage2;

[TestClass]
public class VariableDefinitionTest
{

    [TestMethod]
    public void ParseVariableDefinition()
    {
        //Arrange
        var ast = AstVarDefinition(
            AstTypeIdentifier("int", false), 
            AstIdentifier("test"),
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

        var initializer = AstLiteral(5);
        var ast = AstVarDefinition(
            AstTypeIdentifier("int", false), 
            AstIdentifier("test"),
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
        var ast = AstVarDefinition(
            AstTypeIdentifier("dummy", false), 
            AstIdentifier("test"),
            null);

        var typeScope = new Scope<ICppType>();
        typeScope.TryBindSymbol("int", new CppInt32Type());
        
        var scope = new Scope<ICppValueBase>();
        
        //Act
        
        //Assert
        Should.Throw<ParserException>(() => Stage2Parser.ParseVarDefinition(ast, scope, typeScope))
            .BaseMessage.ShouldBe($"Unknown type 'dummy'");
    }
}