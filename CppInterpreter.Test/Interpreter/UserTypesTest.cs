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
public class UserTypesTest
{


    [TestMethod]
    public void WithField()
    {
        //Arrange
        var ast = new AstCompoundTypeDefinition(
            AstIdentifier("testType"),
            [],
            [],
            [
                AstMemberValue(AstVisibility.Public, "int", "testMember")
            ],
            AstCompoundTypeDefinition.TypeKind.Class,
            [],
            null,
            AstMetadata.Generated()
        );

        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = new Scope<ICppValue>();

        //Act

        var s1 = Stage1Parser.ParseCompoundTypeDefinition(ast, typeScope);
        var s2 = Stage2Parser.ParseCompoundTypeDefinition(s1, valueScope, typeScope);
        // var s3 = Stage3StatementParser.ParseCom
        
        //Assert
        var userType = s2.Type;

        var userValue = userType.Create().ShouldBeOfType<CppUserValue>();
        userValue.MemberValues.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Key.ShouldBe("testMember")
        );

        var member = userType.GetMembers(CppMemberBindingFlags.PublicInstance).ShouldHaveSingleItem();
        member.Name.ShouldBe("testMember");
        member.GetValue(userValue).ShouldBeOfType<CppInt32Value>();
    }
    
    [TestMethod]
    public void WithField_Initialized()
    {
        //Arrange
        var ast = new AstCompoundTypeDefinition(
            AstIdentifier("testType"),
            [],
            [],
            [
                AstMemberValue(AstVisibility.Public, "int", "testMember", AstLiteral(5))
            ],
            AstCompoundTypeDefinition.TypeKind.Class,
            [],
            null,
            AstMetadata.Generated()
        );

        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = new Scope<ICppValue>();

        //Act

        var s1 = Stage1Parser.ParseCompoundTypeDefinition(ast, typeScope);
        var s2 = Stage2Parser.ParseCompoundTypeDefinition(s1, valueScope, typeScope);
        // var s3 = Stage3StatementParser.ParseCom
        
        //Assert
        var userType = s2.Type;

        var userValue = userType.Create().ShouldBeOfType<CppUserValue>();
        userValue.MemberValues.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Key.ShouldBe("testMember")
        );

        var member = userType.GetMembers(CppMemberBindingFlags.PublicInstance).ShouldHaveSingleItem();
        member.Name.ShouldBe("testMember");
        member.GetValue(userValue).ShouldBeOfType<CppInt32Value>().Value.ShouldBe(5);
    }
    
}