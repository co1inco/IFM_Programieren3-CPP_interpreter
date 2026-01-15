using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using CppInterpreter.Test.Helper;
using Shouldly;
using static CppInterpreter.Ast.GeneratedAstTreeBuilder;

namespace CppInterpreter.Test.CppParser.Stage2;

[TestClass]
public class FunctionDefinitionTest
{

    [TestMethod]
    public void ParseFunction()
    {
        //Arrange
        var ast = AstFuncDefinition(
            AstIdentifier("test"),
            AstTypeIdentifier("void", false)
        );

        var stage1Scope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValue>();
        
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
        var ast = AstFuncDefinition(
            AstIdentifier("test"),
            AstTypeIdentifier("void", false),
            [ 
                AstFunctionDefinitionParameter(AstIdentifier("param1"), AstTypeIdentifier("int", false) ), 
                AstFunctionDefinitionParameter(AstIdentifier("param2"), AstTypeIdentifier("long", false) ) 
            ]
        );

        var stage1Scope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValue>();
        
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

    [TestMethod]
    public void ParseFunction_MissingReturn()
    {
        //Arrange
        var ast = AstFuncDefinition(
            AstIdentifier("test"),
            AstTypeIdentifier("int", false),
            [ 
                
            ],
            AstBlock([
                AstIf(
                    [
                        AstIfBranch(AstLiteral(false), AstReturn(AstLiteral(5)))
                    ], 
                    null),
                
            ])
        );

        var stage1Scope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValue>();

        //Act
        var func = Stage2Parser.ParseFuncDefinition(ast, scope, stage1Scope);
        
        //Assert
        Should.Throw<ParserException>(() => Stage3StatementParser.BuildFunction(func, scope, stage1Scope))
            .BaseMessage.ShouldContain("Function must return value of type");
    }
    
    [TestMethod]
    public void ParseFunction_AllIfPathsReturn()
    {
        //Arrange
        var ast = AstFuncDefinition(
            AstIdentifier("test"),
            AstTypeIdentifier("int", false),
            [ 
                
            ],
            AstBlock([
                AstIf(
                    [
                        AstIfBranch(AstLiteral(false), AstReturn(AstLiteral(5)))
                    ], 
                    AstBlock([AstReturn(AstLiteral(5))])),
            ])
        );

        var stage1Scope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValue>();

        //Act
        var func = Stage2Parser.ParseFuncDefinition(ast, scope, stage1Scope);
        
        //Assert
        Should.NotThrow(() => Stage3StatementParser.BuildFunction(func, scope, stage1Scope));
    }
    
    [TestMethod]
    public void ParseFunction_LoopNotCertainReturn()
    {
        //Arrange
        var ast = AstFuncDefinition(
            AstIdentifier("test"),
            AstTypeIdentifier("int", false),
            [ 
                
            ],
            AstBlock([
                AstWhile(
                    //This test is technically statically resolvable to always return (not in scope of this project)
                    AstLiteral(true),  
                    [
                        AstReturn(AstLiteral(5))
                    ]),
                
            ])
        );

        var stage1Scope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValue>();

        //Act
        var func = Stage2Parser.ParseFuncDefinition(ast, scope, stage1Scope);
        
        //Assert
        Should.Throw<ParserException>(() => Stage3StatementParser.BuildFunction(func, scope, stage1Scope))
            .BaseMessage.ShouldContain("Function must return value of type");
    }
}