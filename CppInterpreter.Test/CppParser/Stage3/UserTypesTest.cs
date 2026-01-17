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
    public void RegisterNewType()
    {
        //Arrange
        var ast = new AstCompoundTypeDefinition(
            AstIdentifier("testType"),
            [],
            [],
            [],
            AstCompoundTypeDefinition.TypeKind.Class,
            [],
            null,
            AstMetadata.Generated()
        );

        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = new Scope<ICppValue>();

        //Act
        var s1 = Stage1Parser.ParseCompoundTypeDefinition(ast, typeScope);

        //Assert
        typeScope.TryGetSymbol("testType", out _).ShouldBeTrue();
    }

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
        var s2 = Stage2UserTypeParser.ParseCompoundTypeDefinition(s1, valueScope, typeScope);
        // var s3 = Stage3StatementParser.ParseCom
        
        //Assert
        var userType = s2.Type;

        var userValue = userType.Create().ShouldBeOfType<CppUserValue>();
        userValue.MemberValues.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Key.ShouldBe("testMember")
        );

        var member = userType.GetFields(CppMemberBindingFlags.PublicInstance).ShouldHaveSingleItem();
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
        var s2 = Stage2UserTypeParser.ParseCompoundTypeDefinition(s1, valueScope, typeScope);
        // var s3 = Stage3StatementParser.ParseCom
        
        //Assert
        var userType = s2.Type;

        var userValue = userType.Create().ShouldBeOfType<CppUserValue>();
        userValue.MemberValues.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            x => x.Key.ShouldBe("testMember")
        );

        var member = userType.GetFields(CppMemberBindingFlags.PublicInstance).ShouldHaveSingleItem();
        member.Name.ShouldBe("testMember");
        member.GetValue(userValue).ShouldBeOfType<CppInt32Value>().Value.ShouldBe(5);
    }
    
    [TestMethod]
    public void DefaultConstructor()
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
        var s2 = Stage2UserTypeParser.ParseCompoundTypeDefinition(s1, valueScope, typeScope);
        // var s3 = Stage3StatementParser.ParseCom
        
        //Assert
        valueScope.TryGetSymbol("testType", out var callableValue).ShouldBeTrue();
        var callable = callableValue.ShouldBeOfType<CppCallableValue>();
        var instances = callable.Invoke([]).ShouldBeOfType<CppUserValue>();
        instances.MemberValues.TryGetValue("testMember", out var testMemberValue).ShouldBeTrue();
        testMemberValue.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(5);
    }
    
    [TestMethod]
    public void CustomConstructorConstructor()
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
            [
                AstMemberFunction(
                    AstVisibility.Public,
                    AstFuncDefinition(
                        AstIdentifier("ctor"),
                        AstTypeIdentifier("void", false), 
                        [], 
                        AstBlock(
                            AstAssignmentExpr("testMember", AstLiteral(10))  
                        ))
                )
            ],
            null,
            AstMetadata.Generated()
        );
        
        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = new Scope<ICppValue>();

        //Act

        var s1 = Stage1Parser.ParseCompoundTypeDefinition(ast, typeScope);
        var s2 = Stage2UserTypeParser.ParseCompoundTypeDefinition(s1, valueScope, typeScope);
        var s3 = Stage3Parser.ParseCompoundTypeDefinition(s2, typeScope);
        // var s3 = Stage3StatementParser.ParseCom
        
        //Assert
        valueScope.TryGetSymbol("testType", out var callableValue).ShouldBeTrue();
        var callable = callableValue.ShouldBeOfType<CppCallableValue>();
        var instances = callable.Invoke([]).ShouldBeOfType<CppUserValue>();
        instances.MemberValues.TryGetValue("testMember", out var testMemberValue).ShouldBeTrue();
        testMemberValue.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(10);
    }
    
    [TestMethod]
    public void MemberFunction()
    {
        //Arrange
        var ast = new AstCompoundTypeDefinition(
            AstIdentifier("testType"),
            [],
            [
                AstMemberFunction(
                    AstVisibility.Public,
                    AstFuncDefinition(
                        AstIdentifier("foo"),
                        AstTypeIdentifier("void", false)
                    )
                )
            ],
            [],
            AstCompoundTypeDefinition.TypeKind.Class,
            [],
            null,
            AstMetadata.Generated()
        );
        
        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = new Scope<ICppValue>();

        //Act

        var s1 = Stage1Parser.ParseCompoundTypeDefinition(ast, typeScope);
        var s2 = Stage2UserTypeParser.ParseCompoundTypeDefinition(s1, valueScope, typeScope);
        var s3 = Stage3Parser.ParseCompoundTypeDefinition(s2, typeScope);
        // var s3 = Stage3StatementParser.ParseCom
        
        //Assert

        var userType = s1.Type;
        var functions = userType.GetFunctions(CppMemberBindingFlags.Public);
        functions.FirstOrDefault(x => x.Name == "foo")
            .ShouldNotBeNull()
            .ShouldSatisfyAllConditions(
                x => x.Name.ShouldBe("foo"),
                x => x.MemberType.ShouldBeOfType<CppCallableType>()
            );

        ((ICppType)userType).GetMembers(CppMemberBindingFlags.Public)
            .FirstOrDefault(x => x.Name == "foo")
            .ShouldNotBeNull()
            .ShouldBeOfType<CppMemberFunctionInfo>();
    }
}