using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using NSubstitute;
using static CppInterpreter.Ast.GeneratedAstTreeBuilder;

namespace CppInterpreter.Test.CppParser.Stage3;

[TestClass]
public class IfTest
{

    [TestMethod]
    public void Branch_Single()
    {
        //Arrange
        var ast = AstIf(
            [
                AstIfBranch(
                    AstLiteral(true),
                    (AstExpression)AstFunctionCall("test", [ AstLiteral(1) ]))
            ],
            AstBlock([
                (AstExpression)AstFunctionCall("test", [ AstLiteral(2) ])
            ])
        );

        var testFunction = Substitute.For<ICppFunction>();
        testFunction.ParameterTypes.Returns([new CppFunctionParameter("x", CppTypes.Int32, false)]);

        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = Stage2Parser.CreateBaseScope();
        var parseScope = new Scope<ICppValue>(valueScope);

        valueScope.BindFunction(testFunction, "test");
        
        //Act
        var s3 = Stage3Parser.ParseIf(ast, parseScope, typeScope);
        var result = s3.StatementEval(valueScope);
        
        //Assert
        testFunction.Received(1).Invoke(null, FirstArgIs<ICppValue[]>(1));
    }
    
    [TestMethod]
    public void Branch_ElseIf()
    {
        //Arrange
        var ast = AstIf(
            [
                AstIfBranch(
                    AstLiteral(false),
                    (AstExpression)AstFunctionCall("test", [ AstLiteral(1) ])),
                AstIfBranch(
                    AstLiteral(true),
                    (AstExpression)AstFunctionCall("test", [ AstLiteral(2) ]))
            ],
            AstBlock([
                (AstExpression)AstFunctionCall("test", [ AstLiteral(3) ])
            ])
        );

        var testFunction = Substitute.For<ICppFunction>();
        testFunction.ParameterTypes.Returns([new CppFunctionParameter("x", CppTypes.Int32, false)]);

        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = Stage2Parser.CreateBaseScope();
        var parseScope = new Scope<ICppValue>(valueScope);

        valueScope.BindFunction(testFunction, "test");
        
        //Act
        var s3 = Stage3Parser.ParseIf(ast, parseScope, typeScope);
        var result = s3.StatementEval(valueScope);
        
        //Assert
        testFunction.Received(1).Invoke(null, FirstArgIs<ICppValue[]>(2));
    }

    [TestMethod]
    public void Branch_Else()
    {
        //Arrange
        var ast = AstIf(
            [
                AstIfBranch(
                    AstLiteral(false),
                    (AstExpression)AstFunctionCall("test", [ AstLiteral(1) ])),
                AstIfBranch(
                AstLiteral(false),
                (AstExpression)AstFunctionCall("test", [ AstLiteral(2) ]))
            ],
            AstBlock([
                (AstExpression)AstFunctionCall("test", [ AstLiteral(3) ])
            ])
        );

        var testFunction = Substitute.For<ICppFunction>();
        testFunction.ParameterTypes.Returns([new CppFunctionParameter("x", CppTypes.Int32, false)]);

        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = Stage2Parser.CreateBaseScope();
        var parseScope = new Scope<ICppValue>(valueScope);

        valueScope.BindFunction(testFunction, "test");
        
        //Act
        var s3 = Stage3Parser.ParseIf(ast, parseScope, typeScope);
        var result = s3.StatementEval(valueScope);
        
        //Assert
        testFunction.Received(1).Invoke(null, FirstArgIs<ICppValue[]>(3));
    }
    

    public static ICppValue[] FirstArgIs<T>(int value) => Arg.Is<ICppValue[]>(x =>
        x.Length == 1
        && x.First() is CppInt32Value
        && ((CppInt32Value)x.First()).Value == value
    );

}