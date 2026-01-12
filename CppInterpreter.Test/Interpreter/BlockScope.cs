using CppInterpreter.CppParser;
using CppInterpreter.Interpreter.Values;
using Shouldly;

namespace CppInterpreter.Test.Interpreter;
using static CppInterpreter.Ast.GeneratedAstTreeBuilder;

[TestClass]
public class BlockScope
{

    [TestMethod]
    public void ShadowVariable()
    {
        //Arrange
        var ast = AstBlock([
            AstVarDefinition(
                AstTypeIdentifier("int", false), 
                AstIdentifier("test"),
                AstLiteral(42))
        ]);

        var typeScope = Stage1Parser.CreateBaseScope();
        var scope = Stage2Parser.CreateBaseScope();

        var value = new CppInt32Value(5);
        scope.TryBindSymbol("test", value);
        
        var statement = Stage3StatementParser.ParseBlock(ast, scope, typeScope);
        
        //Act
        var result = statement.StatementEval(scope);

        //Assert
        value.Value.ShouldBe(5);
    }
    
    [TestMethod]
    public void ShadowVariable_DifferentType()
    {
        //Arrange
        var ast = AstBlock([
            AstVarDefinition(
                AstTypeIdentifier("int", false), 
                AstIdentifier("test"),
                AstLiteral(42))
        ]);

        var typeScope = Stage1Parser.CreateBaseScope();
        var scope = Stage2Parser.CreateBaseScope();

        var value = new CppBoolValue(true);
        scope.TryBindSymbol("test", value);
        
        var statement = Stage3StatementParser.ParseBlock(ast, scope, typeScope);
        
        //Act
        var result = statement.StatementEval(scope);

        //Assert
        value.Value.ShouldBe(true);
    }
    
}