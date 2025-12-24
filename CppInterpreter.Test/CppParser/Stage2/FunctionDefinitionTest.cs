using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using CppInterpreter.Test.Helper;
using Shouldly;

namespace CppInterpreter.Test.CppParser.Stage2;

[TestClass]
public class FunctionDefinitionTest
{

    [TestMethod]
    public void ParseFunction()
    {
        //Arrange
        var ast = new AstFuncDefinition(
            new AstIdentifier("test"),
            new AstTypeIdentifier("void", false),
            [],
            []
        );

        var stage1Scope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValueBase>();
        
        //Act
        var result = Stage2Parser.ParseFuncDefinition(ast, scope, stage1Scope);

        //Assert
        scope.TryGetSymbol("test", out var test).ShouldBeTrue();
        test.ShouldBeOfType<CppCallableValue>()
            .Overloads.ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                x => x.Name.ShouldBe("test"),
                x => x.ParameterTypes.ShouldBeEmpty());
        
        result.Name.ShouldBe("test");
        result.ReturnType.ShouldBeOfType<CppVoidType>();
        result.Arguments.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void ParseFunction_WithArguments()
    {
        //Arrange
        var ast = new AstFuncDefinition(
            new AstIdentifier("test"),
            new AstTypeIdentifier("void", false),
            [ 
                new AstFunctionDefinitionParameter(new AstIdentifier("param1"), new AstTypeIdentifier("int", false) ), 
                new AstFunctionDefinitionParameter(new AstIdentifier("param2"), new AstTypeIdentifier("long", false) ) 
            ],
            []
        );

        var stage1Scope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValueBase>();
        
        //Act
        var result = Stage2Parser.ParseFuncDefinition(ast, scope, stage1Scope);

        //Assert
        scope.TryGetSymbol("test", out var test).ShouldBeTrue();
        test.ShouldBeOfType<CppCallableValue>()
            .Overloads.ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                x => x.Name.ShouldBe("test"),
                x => x.ParameterTypes.ShouldHaveCount(2));
        
        result.Name.ShouldBe("test");
        result.ReturnType.ShouldBeOfType<CppVoidType>();
        result.Arguments.ShouldHaveCount(2);
        
        result.Arguments[0].ShouldSatisfyAllConditions(
            x => x.Name.ShouldBe("param1"),
            x => x.Type.ShouldBeOfType<CppInt32Type>()
        );
        
        result.Arguments[1].ShouldSatisfyAllConditions(
            x => x.Name.ShouldBe("param2"),
            x => x.Type.ShouldBeOfType<CppInt64Type>()
        );
    }
    
}