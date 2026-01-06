using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
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
            [],
            AstBlock([
                    AstFunctionCallExpr(AstAtom("foo"), [])
            ])
        );
        
        var ast = new Stage2FuncDefinition(
            "test",
            CppTypes.Void,
            [], 
            AstBlock([]), 
            userFunction, 
            scope);

        //Act
        var stmt = Stage3Parser.BuildFunction(ast, scope, typeScope);
        
        //Assert
        stmt.Eval(scope).IsNone.ShouldBeTrue();
        userFunction.Invoke(null, []);

        dummyFunction.Received(1).Invoke(null, []);
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