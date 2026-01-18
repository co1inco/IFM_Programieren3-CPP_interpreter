using Antlr4.Runtime.Misc;
using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using NSubstitute;
using Shouldly;
using static CppInterpreter.Ast.GeneratedAstTreeBuilder;

namespace CppInterpreter.Test.CppParser.Stage3;

[TestClass]
public class UserFunctionsTest
{


    [TestMethod]
    public void BuildUserFunction()
    {
        //Arrange

        var typeScope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValue>();

        var dummyFunction = Substitute.For<ICppFunction>();
        scope.TryBindSymbol("foo", new CppCallableValue(scope)
        {
            Overloads = { dummyFunction }
        });
        
        var userFunction = new CppUserFunction(
            "test",
            CppTypes.Void,
            null,
            []
        );
        
        var ast = new Stage2FuncDefinition( 
            AstBlock([
                AstFunctionCallExpr(AstAtom("foo"), [])
            ]), 
            userFunction);

        //Act
        var stmt = Stage3StatementParser.ParseStage2FunctionDefinition(ast, scope, typeScope);
        
        //Assert
        stmt.StatementEval(scope).IsNone.ShouldBeTrue();
        userFunction.Invoke(null, []);

        dummyFunction.Received(1).Invoke(null, []);
    }
    
    [TestMethod]
    public void BuildInstanceUserFunction()
    {
        //Arrange

        var typeScope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValue>();

        var dummyFunction = Substitute.For<ICppFunction>();
        dummyFunction.ParameterTypes.Returns([
            new CppFunctionParameter("", CppTypes.Int32, false)
        ]);
        scope.TryBindSymbol("dummyFunction", new CppCallableValue(scope)
        {
            Overloads = { dummyFunction }
        });

        var dummyInstance = CreateDummyInstance(scope, typeScope);
        
        var userFunction = new CppUserFunction(
            "test",
            CppTypes.Void,
            dummyInstance.GetCppType,
            []
        );
        
        var ast = new Stage2FuncDefinition(
            AstBlock([
                AstFunctionCallExpr(AstAtom("dummyFunction"), [AstAtom("instanceMember")])
            ]), 
            userFunction
        );

        //Act
        var stmt = Stage3StatementParser.ParseStage2FunctionDefinition(ast, scope, typeScope);
        
        //Assert
        stmt.StatementEval(scope).IsNone.ShouldBeTrue();
        userFunction.Invoke(dummyInstance, []);

        dummyFunction.Received(1).Invoke(
            null, 
            Arg.Is<ICppValue[]>(i => 
                i.Length == 1 
                && i[0] is CppInt32Value
            ));
    }


    private ICppValue CreateDummyInstance(Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        var instanceType = new CppUserType("dummyType");
        instanceType.BuildMembers(scope, ((builder, s) =>
        {
            builder.AddVariable("instanceMember", CppTypes.Int32, null, MemberVisibility.Private);
            builder.AddConstructor(new BaseUserTypeConstructor(
                instanceType, s, [], null
            ));
        }));
        
        return instanceType.Create();
    }
    
    // [TestMethod]
    // public void RespectLocalScope()
    // {
    //     //Arrange
    //
    //     var typeScope = Stage1Parser.CreateBaseScope();
    //     var scope = new Scope<ICppValueBase>();
    //     
    //     var userFunction = new CppUserFunction(
    //         "test",
    //         new CppVoidType(),
    //         [ ("x", new CppInt32Type()) ],
    //         [
    //             new AstStatement((AstExpression) new AstAssignment(new AstIdentifier("x"), new AstLiteral(5))
    //         )]
    //     );
    //     
    //     var ast = new Stage2FuncDefinition(
    //         "test",
    //         new CppVoidType(),
    //         [ ("x", new CppInt32Type()) ], 
    //         [], 
    //         userFunction, 
    //         scope);
    //
    //     var callerScope = new Scope<ICppValueBase>();
    //     
    //     var value = new CppInt32Value(42);
    //     callerScope.TryBindSymbol("foo", value);
    //     
    //     //Act
    //     var stmt = Stage3Parser.BuildFunction(ast, typeScope);
    //     
    //     //Assert
    //     stmt.Invoke(scope).ShouldBeOfType<CppVoidValue>();
    //     userFunction.Invoke(null, [ value ]);
    //
    //     value.Value.ShouldBe(42);
    // }
    
}