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
public class ParseAssignmentTest
{


    [TestMethod]
    public void ParseAssignment()
    {
        //Arrange
        var value = new CppInt32Value(0);
        
        var scope = new Scope<ICppValueBase>();
        scope.TryBindSymbol("test", value);
        
        var ast = AstAssignment(
            AstIdentifier("test"),
            AstLiteral(5)
        );
        
        //Act
        var expr = Stage3Parser.ParseAssignment(ast, scope);
        var result = expr.Eval(scope);
        
        //Assert
        result.ShouldBe(value);
        value.Value.ShouldBe(5);
    }


    [TestMethod]
    public void UseOperator()
    {
        //Arrange

        var assignmentOperator = Substitute.For<ICppFunction>();
        assignmentOperator.Name.Returns("operator=");
        assignmentOperator
            .When(x => x.Invoke(Arg.Any<ICppValueBase>(), Arg.Any<ICppValueBase[]>()))
            .Do((_ => {}));
        
        var type = Substitute.For<ICppType>();
        type.Name.Returns("dummy");
        type.Equals(type).Returns(true);
        type.Equals(CppTypes.Int32).Returns(true);
        type.Functions.Returns([
            assignmentOperator
        ]);
        
        var value =  Substitute.For<ICppValueBase>();
        value.Type.Returns(type);
        
        var scope = new Scope<ICppValueBase>();
        scope.TryBindSymbol("test", value);
        
        assignmentOperator.InstanceType.Returns(type);
        assignmentOperator.ParameterTypes.Returns([ new CppFunctionParameter("", new CppInt32Type(), false) ]);
        
        var ast = AstAssignment(
            AstIdentifier("test"),
            AstLiteral(5)
        );
        
        //Act
        var expr = Stage3Parser.ParseAssignment(ast, scope);
        var result = expr.Eval(scope);
        
        //Assert
        assignmentOperator.Received(1).Invoke(Arg.Any<ICppValueBase>(), Arg.Any<ICppValueBase[]>());
    }

}