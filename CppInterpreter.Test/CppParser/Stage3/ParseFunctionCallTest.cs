using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using NSubstitute;
using Shouldly;

namespace CppInterpreter.Test.CppParser.Stage3;

[TestClass]
public class ParseFunctionCallTest
{

    [TestMethod]
    public void ParseFunctionCall_NoArguments()
    {
        //Arrange
        var ast = new AstFunctionCall(
            (AstExpression)new AstAtom("test"),
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
        var ast = new AstFunctionCall(
            (AstExpression)new AstAtom("test"),
            [ new AstLiteral(42) ]);

        var parameter = new CppInt32Value(42);
        
        var scope = new Scope<ICppValueBase>();
        
        var function = Substitute.For<ICppFunction>();
        function.Name.Returns("test");
        function.InstanceType.Returns((ICppType?)null);
        function.ReturnType.Returns(new CppVoidType());
        function.ParameterTypes.Returns([ new CppInt32Type() ]);
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
        var ast = new AstFunctionCall(
            (AstExpression)new AstAtom("test"),
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
    
}