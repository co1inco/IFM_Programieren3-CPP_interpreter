using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using NSubstitute;
using Shouldly;

namespace CppInterpreter.Test.CppParser;

[TestClass]
public class ParseAssignmentTest
{


    [TestMethod]
    public void ParseAssignment()
    {
        //Arrange
        var value = new CppInt32Value(0);
        
        var valueScope = new Scope<ICppValueBase>();
        valueScope.TryBindSymbol("test", value);
        
        var scope = new CppStage1Scope()
        {
            Types = null!,
            Values =  valueScope
        };

        var ast = new AstAssignment(
            new AstIdentifier("test"),
            new AstLiteral(5)
        );
        
        //Act
        var expr = CppInterpreter.CppParser.CppParser.ParseAssignment(ast, scope);

        var eval = expr.Evaluate();
        
        //Assert
        eval.ShouldBe(value);
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
        
        var valueScope = new Scope<ICppValueBase>();
        valueScope.TryBindSymbol("test", value);
        
        assignmentOperator.InstanceType.Returns(type);
        assignmentOperator.ParameterTypes.Returns([new CppInt32Type()]);
        
        var scope = new CppStage1Scope()
        {
            Types = null!,
            Values =  valueScope
        };

        var ast = new AstAssignment(
            new AstIdentifier("test"),
            new AstLiteral(5)
        );
        
        //Act
        var expr = CppInterpreter.CppParser.CppParser.ParseAssignment(ast, scope);

        var eval = expr.Evaluate();
        
        
        //Assert
    }

    public abstract class DummyValue : ICppValue
    {
        public abstract ICppType Type { get; }

        public abstract string StringRep();

        public static ICppType SType { get; }
    }
}