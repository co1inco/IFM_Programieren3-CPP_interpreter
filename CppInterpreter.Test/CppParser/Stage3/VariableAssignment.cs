using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using Shouldly;

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
        
        var ast = new AstVarDefinition(
            new AstTypeIdentifier("int", true),
            new AstIdentifier("test"),
            null);

        //Act / Assert
        Should.Throw<Exception>(() => Stage3Parser.ParseVariableDefinition(ast, typeScope))
            .Message.ShouldBe($"Declaration of reference variable 'test' required an initializer");
        
    }
    
    [TestMethod]
    public void ReferenceVariable_ChangeReference()
    {
        //Arrange
        var typeScope = Stage1Parser.CreateBaseScope();
        
        var ast = new AstVarDefinition(
            new AstTypeIdentifier("int", true),
            new AstIdentifier("test"),
            new AstAtom("baseVal"));

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