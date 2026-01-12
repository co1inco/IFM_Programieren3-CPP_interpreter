using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using NSubstitute;
using Shouldly;
using static CppInterpreter.Ast.GeneratedAstTreeBuilder;

namespace CppInterpreter.Test.Interpreter;

[TestClass]
public class MemberAccessTest
{


    [TestMethod]
    public void GetMember()
    {
        //Arrange
        var ast = AstMemberAccess(AstAtom("value"), "member");

        // var typeScope = new Scope<ICppType>();
        var valueScope =  new Scope<ICppValue>();

        var member = Substitute.For<ICppMemberInfo>();
        
        var type = Substitute.For<ICppType>();
        type.GetMember("member", Arg.Any<CppMemberBindingFlags>())
            .Returns(member);
        
        var value = Substitute.For<ICppValue>();
        value.GetCppType.Returns(type);
        valueScope.TryBindSymbol("value", value);
        
        //Act
        var s3 = Stage3ExpressionParser.ParseExpression(ast, valueScope);

        //Assert

        type.Received(1).GetMember("member", CppMemberBindingFlags.Instance | CppMemberBindingFlags.Public);
    }
    
    [TestMethod]
    public void StringLength()
    {
        //Arrange
        var ast = AstFunctionCall(
            (AstExpression)AstMemberAccess(AstAtom("value"), "size"),
            []
        );

        // var typeScope = new Scope<ICppType>();
        var valueScope =  new Scope<ICppValue>();

        var value = new CppStringValue("hello");
        valueScope.TryBindSymbol("value", value);
        
        //Act
        var s3 = Stage3ExpressionParser.ParseExpression(ast, valueScope);
        var result = s3.Eval(valueScope);

        //Assert
        result.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(5);
    }
}