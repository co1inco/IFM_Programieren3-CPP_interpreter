using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using NSubstitute;
using Shouldly;

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
        
        var ast = new AstAssignment(
            new AstIdentifier("test"),
            new AstLiteral(5)
        );
        
        //Act
        var expr = Stage3Parser.ParseAssignment(ast);
        var result = expr(scope);
        
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
        type.Functions.Returns([
            assignmentOperator
        ]);
        
        var value =  Substitute.For<ICppValueBase>();
        value.Type.Returns(type);
        
        var scope = new Scope<ICppValueBase>();
        scope.TryBindSymbol("test", value);
        
        assignmentOperator.InstanceType.Returns(type);
        assignmentOperator.ParameterTypes.Returns([new CppInt32Type()]);
        
        var ast = new AstAssignment(
            new AstIdentifier("test"),
            new AstLiteral(5)
        );
        
        //Act
        var expr = Stage3Parser.ParseAssignment(ast);
        var result = expr(scope);
        
        //Assert
        assignmentOperator.Received(1).Invoke(Arg.Any<ICppValueBase>(), Arg.Any<ICppValueBase[]>());
    }

    public abstract class DummyValue : ICppValue
    {
        public abstract ICppType Type { get; }

        public abstract string StringRep();

        public static ICppType SType { get; }
    }
}