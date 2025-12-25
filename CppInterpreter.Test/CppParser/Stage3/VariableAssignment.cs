using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using Shouldly;
using static CppInterpreter.Ast.GeneratedAstTreeBuilder;

namespace CppInterpreter.Test.CppParser.Stage3;

[TestClass]
public class VariableAssignment
{

    [TestMethod]
    public void AssignReferenceVariable()
    {
        //Arrange
        

        //Act

        //Assert
    }
    
    
    [TestMethod]
    public void ReferenceVariable_NoInitializer()
    {
        //Arrange
        var typeScope = Stage1Parser.CreateBaseScope();
        
        var ast = AstVarDefinition(
            AstTypeIdentifier("int", true),
            AstIdentifier("test"),
            null);

        //Act / Assert
        Should.Throw<ParserException>(() => Stage3Parser.ParseVariableDefinition(ast, typeScope));
            // .BaseMessage.ShouldBe($"Declaration of reference variable 'test' required an initializer");
        
    }
    
    [TestMethod]
    public void ReferenceVariable_ChangeReference()
    {
        //Arrange
        var typeScope = Stage1Parser.CreateBaseScope();
        
        var ast = AstVarDefinition(
            AstTypeIdentifier("int", true),
            AstIdentifier("test"),
            AstAtom("baseVal"));

        var baseVal = new CppInt32Value(42);
        
        var scope = new Scope<ICppValueBase>();
        scope.TryBindSymbol("baseVal", baseVal);

        //Act / Assert 
        var expr =  Stage3Parser.ParseVariableDefinition(ast, typeScope);
        expr(scope);

        scope.TryGetSymbol("test", out var test).ShouldBeTrue();
        var testVal = test.ShouldBeOfType<CppInt32Value>();
        
        testVal.Value.ShouldBe(baseVal.Value);
        testVal.Value = 5;
        
        baseVal.Value.ShouldBe(5);
    }
    
}