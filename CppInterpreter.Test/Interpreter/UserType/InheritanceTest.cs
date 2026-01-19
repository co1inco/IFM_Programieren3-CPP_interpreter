using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using Shouldly;

namespace CppInterpreter.Test.Interpreter.UserType;

[TestClass]
public class InheritanceTest
{


    [TestMethod]
    public void GetInheritedMember()
    {
        //Arrange
        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = new Scope<ICppValue>();
        
        var baseType = new CppUserType("baseType");
        baseType.BuildMembers(valueScope, (b, closure) =>
        {
            b.AddVariable(
                "baseValue",
                CppTypes.Int32,
                new InterpreterExpressionResult(_ => CppInt32Value.Create(5), CppTypes.Int32),
                MemberVisibility.Public
            );
        });
        
        //Act
        var derivedType = new CppUserType("derivedType");
        derivedType.BuildMembers(valueScope, (b, closure) =>
        {
            b.AddBaseType(baseType, MemberVisibility.Public);
        });
        
        //Assert
        derivedType.GetMembers(CppMemberBindingFlags.Public)
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                x => x.Name.ShouldBe("baseValue")
            );
    }
    
    [TestMethod]
    public void GetInheritedValue()
    {
        //Arrange
        var typeScope = Stage1Parser.CreateBaseScope();
        var valueScope = new Scope<ICppValue>();
        
        var baseType = new CppUserType("baseType");
        baseType.BuildMembers(valueScope, (b, closure) =>
        {
            b.AddVariable(
                "baseValue",
                CppTypes.Int32,
                new InterpreterExpressionResult(_ => CppInt32Value.Create(5), CppTypes.Int32),
                MemberVisibility.Public
            );
        });
        
        var derivedType = new CppUserType("derivedType");
        derivedType.BuildMembers(valueScope, (b, closure) =>
        {
            b.AddBaseType(baseType, MemberVisibility.Public);
        });

        var inheritedMember = derivedType.GetFields(CppMemberBindingFlags.Public).First(x => x.Name == "baseValue");
        var instance = derivedType.Create();
        
        //Act
        var value = inheritedMember.GetValue(instance);
        
        //Assert
        value.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(5);
    }
    
}