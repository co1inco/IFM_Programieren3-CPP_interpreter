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
public class FunctionCallTest
{

    [TestMethod]
    public void ParseFunctionCall_NoArguments()
    {
        //Arrange
        var ast = AstFunctionCall(
            (AstExpression)AstAtom("test"),
            []);

        var scope = new Scope<ICppValueBase>();
        
        var function = Substitute.For<ICppFunction>();
        function.Name.Returns("test");
        function.InstanceType.Returns((ICppType?)null);
        function.ReturnType.Returns(new CppVoidType());
        function.ParameterTypes.Returns([]);
        function.Invoke(null, []).Returns(new CppVoidValue());
        
        var callable = new CppCallableValue(scope);
        callable.AddOverload(function);
        
        scope.TryBindSymbol("test", callable);

        
        //Act
        var expr = Stage3Parser.ParseExpression(ast);
        var result = expr(scope);

        //Assert
        result.ShouldBeOfType<CppVoidValue>();
        function.Received(1).Invoke(null, []);
    }
    
    [TestMethod]
    public void ParseFunctionCall_WithArgument()
    {
        //Arrange
        var ast = AstFunctionCall(
            AstAtom("test"),
            [ AstLiteral(42) ]);

        var parameter = new CppInt32Value(42);
        
        var scope = new Scope<ICppValueBase>();
        
        var function = Substitute.For<ICppFunction>();
        function.Name.Returns("test");
        function.InstanceType.Returns((ICppType?)null);
        function.ReturnType.Returns(new CppVoidType());
        function.ParameterTypes.Returns([ new CppFunctionParameter("", new CppInt32Type(), false) ]);
        function.Invoke(null, Arg.Any<ICppValueBase[]>()).Returns(new CppVoidValue());
        
        var callable = new CppCallableValue(scope);
        callable.AddOverload(function);
        
        scope.TryBindSymbol("test", callable);

        
        //Act
        var expr = Stage3Parser.ParseExpression(ast);
        var result = expr(scope);

        //Assert
        result.ShouldBeOfType<CppVoidValue>();
        function.Received(1).Invoke(null, Arg.Is<ICppValueBase[]>( (ICppValueBase[] x) => x.Length == 1));
        
    }
    
    [TestMethod]
    public void ParseFunctionCall_WithReturn()
    {
        //Arrange
        var ast = AstFunctionCall(
            AstAtom("test"),
            []);

        var scope = new Scope<ICppValueBase>();
        
        var function = Substitute.For<ICppFunction>();
        function.Name.Returns("test");
        function.InstanceType.Returns((ICppType?)null);
        function.ReturnType.Returns(new CppInt32Type());
        function.ParameterTypes.Returns([]);
        function.Invoke(null, []).Returns(new CppInt32Value(42));
        
        var callable = new CppCallableValue(scope);
        callable.AddOverload(function);
        
        scope.TryBindSymbol("test", callable);

        
        //Act
        var expr = Stage3Parser.ParseExpression(ast);
        var result = expr(scope);

        //Assert
        result.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(42);
        function.Received(1).Invoke(null, []);
    }
    
    [Ignore] // Internal functions always pass by reference
    [TestMethod]
    public void FunctionCall_RespectLocalScope_Internal()
    {
        //Arrange
        var ast = AstFunctionCall(
            AstAtom("test"),
            [ AstAtom("value") ]);

        var scope = new Scope<ICppValueBase>();
        
        var function = Substitute.For<ICppFunction>();
        function.Name.Returns("test");
        function.InstanceType.Returns((ICppType?)null);
        function.ReturnType.Returns(new CppVoidType());
        function.ParameterTypes.Returns([ new CppFunctionParameter("", new CppInt32Type(), false) ]);
        function.Invoke(null, Arg.Any<ICppValueBase[]>())
            .Returns(x =>
            {
                var value = x.Arg<ICppValueBase[]>()[0];
                
                value.ShouldBeOfType<CppInt32Value>().Value = 5;
                
                return new CppVoidValue();
            });
        
        var callable = new CppCallableValue(scope);
        callable.AddOverload(function);
        
        scope.TryBindSymbol("test", callable);

        
        var callingScope = new Scope<ICppValueBase>(scope);
        var value = new CppInt32Value(42);
        callingScope.TryBindSymbol("value", value);
        
        //Act
        var expr = Stage3Parser.ParseExpression(ast);
        var result = expr(callingScope);

        //Assert
        value.Value.ShouldBe(42);
    }
    
    [TestMethod]
    public void FunctionCall_ReferenceParameter()
    {
        //Arrange
        var ast = AstFunctionCall(
            AstAtom("test"),
            [ AstAtom("value") ]);

        var scope = new Scope<ICppValueBase>();
        
        var function = Substitute.For<ICppFunction>();
        function.Name.Returns("test");
        function.InstanceType.Returns((ICppType?)null);
        function.ReturnType.Returns(new CppVoidType());
        function.ParameterTypes.Returns([ new CppFunctionParameter("p1", new CppInt32Type(), true) ]);
        function.Invoke(null, Arg.Any<ICppValueBase[]>())
            .Returns(x =>
            {
                var value = x.Arg<ICppValueBase[]>()[0];
                
                value.ShouldBeOfType<CppInt32Value>().Value = 5;
                
                return new CppVoidValue();
            });
        
        var callable = new CppCallableValue(scope);
        callable.AddOverload(function);
        
        scope.TryBindSymbol("test", callable);

        
        var callingScope = new Scope<ICppValueBase>(scope);
        var value = new CppInt32Value(42);
        callingScope.TryBindSymbol("value", value);
        
        //Act
        var expr = Stage3Parser.ParseExpression(ast);
        var result = expr(callingScope);

        //Assert
        value.Value.ShouldBe(5);
    }
    
    [TestMethod]
    public void FunctionCall_RespectLocalScope()
    {
        //Arrange
        var ast = AstFunctionCall(
            AstAtom("test"),
            [ AstAtom("value") ]);

        var functionAst = AstFuncDefinition(
            AstIdentifier("test"),
            AstTypeIdentifier("void", false),
            [
                AstFunctionDefinitionParameter(
                AstIdentifier("x"),
                AstTypeIdentifier("int", false)),
            ],
            AstBlock([
                AstAssignmentExpr(
                    AstIdentifier("x"),
                    AstLiteral(5))
            ])
        );

        var typeScope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValueBase>();
        
        var fnDef = Stage2Parser.ParseFuncDefinition(functionAst, scope, typeScope);
        Stage3Parser.BuildFunction(fnDef, typeScope);
        
        var callingScope = new Scope<ICppValueBase>(scope);
        var value = new CppInt32Value(42);
        callingScope.TryBindSymbol("value", value);
        
        //Act
        
        
        
        var expr = Stage3Parser.ParseExpression(ast);
        var result = expr(callingScope);

        //Assert
        value.Value.ShouldBe(42);
    }
    
    [TestMethod]
    public void FunctionCall_PassCopyParameter()
    {
        //Arrange
        var ast = AstFunctionCall(
            AstAtom("test"),
            [ AstAtom("value") ]);

        var functionAst = AstFuncDefinition(
            AstIdentifier("test"),
            AstTypeIdentifier("void", false),
            [
                AstFunctionDefinitionParameter(
                    AstIdentifier("x"),
                    AstTypeIdentifier("int", false)),
            ],
            AstBlock([
                (AstExpression)AstFunctionCall(AstAtom("check"), [ AstAtom("x") ])
            ])
        );

        var function = Substitute.For<ICppFunction>();
        function.Name.Returns("x");
        function.InstanceType.Returns((ICppType?)null);
        function.ReturnType.Returns(new CppVoidType());
        function.ParameterTypes.Returns([ new CppFunctionParameter("p1", new CppInt32Type(), true) ]); 
        
        
        var typeScope = Stage1Parser.CreateBaseScope();
        var scope = new Scope<ICppValueBase>();

        var callable = new CppCallableValue(scope);
        callable.AddOverload(function);
        scope.TryBindSymbol("check", callable);
        
        var fnDef = Stage2Parser.ParseFuncDefinition(functionAst, scope, typeScope);
        Stage3Parser.BuildFunction(fnDef, typeScope);
        
        var callingScope = new Scope<ICppValueBase>(scope);
        var value = new CppInt32Value(42);
        callingScope.TryBindSymbol("value", value);
        
        //Act
        
        
        
        var expr = Stage3Parser.ParseExpression(ast);
        var result = expr(callingScope);

        //Assert
        value.Value.ShouldBe(42);
        function.Received(1).Invoke(null, Arg.Is<ICppValueBase[]>(v => v[0] is CppInt32Value && ((CppInt32Value)v[0]).Value == 42));
    }
}