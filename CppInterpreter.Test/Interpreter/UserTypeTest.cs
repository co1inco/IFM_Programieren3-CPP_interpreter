using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using NSubstitute;
using Shouldly;

namespace CppInterpreter.Test.Interpreter;

[TestClass]
public class UserTypeTest
{


    [TestMethod]
    public void UpdateMember() //Note: not sure if this tests anything useful
    {
        //Arrange

        var memberValue = CppInt32Value.Create(42);
        
        var type = new CppUserType("test");
        
        var memberFunction = Substitute.For<ICppFunction>();
        memberFunction.InstanceType.Returns(type);
        
        type.BuildMembers(new Scope<ICppValue>(), (builder, scope) =>
        {
            builder.AddVariable("x", CppTypes.Int32, null, MemberVisibility.Public);
            builder.AddFunction("foo", memberFunction, MemberVisibility.Public);
            
            builder.AddConstructor(new BaseUserTypeConstructor(
                type, 
                scope, 
                [
                    ("x", new InterpreterExpressionResult(s => memberValue, CppTypes.Int32 ))
                ], 
                null));
        });
        var value = type.Create();
        
        //Act
        ((ICppType)type).GetFunction("foo", CppMemberBindingFlags.AnyInstance)!.Invoke(value);
        
        //Assert
        type.GetFields(CppMemberBindingFlags.AnyInstance)
            .First(x => x.Name == "x")
            .GetValue(value)
            .ShouldBe(memberValue);

        memberFunction.Received().Invoke(value, []);
    }
    
    [TestMethod]
    public void UserTypeScope()
    {
        //Arrange

        var memberValue = CppInt32Value.Create(42);
        
        var type = new CppUserType("test");
        
        type.BuildMembers(new Scope<ICppValue>(), (builder, scope) =>
        {
            builder.AddVariable("x", CppTypes.Int32, null, MemberVisibility.Public);
            
            builder.AddConstructor(new BaseUserTypeConstructor(
                type, 
                scope, 
                [
                    ("x", new InterpreterExpressionResult(s => memberValue, CppTypes.Int32 ))
                ], 
                null));
        });
        
        
        //Act
        var value = type.Create();
        
        //Assert
        value.InstanceScope.TryGetSymbol("x", out var testMemberValue).ShouldBeTrue();
        testMemberValue.ShouldBe(memberValue);
    }
    
    
    [TestMethod]
    public void UsedMembersInstance()
    {
        //Arrange

        var memberValue = CppInt32Value.Create(42);
        
        var type = new CppUserType("test");
        
        var constructorFunction = Substitute.For<ICppFunction>();
        constructorFunction.ReturnType.Returns(CppTypes.Void);
        constructorFunction.InstanceType.Returns(type);
        constructorFunction.Invoke(Arg.Any<ICppValue>(), [])
            .Returns(x =>
            {
                var scope = x.ArgAt<ICppValue>(0).InstanceScope;
                return new CppVoidValue();
            });
        
        type.BuildMembers(new Scope<ICppValue>(), (builder, scope) =>
        {
            builder.AddVariable("x", CppTypes.Int32, null, MemberVisibility.Public);
            
            builder.AddConstructor(new BaseUserTypeConstructor(
                type, 
                scope, 
                [
                    ("x", new InterpreterExpressionResult(s => memberValue, CppTypes.Int32 ))
                ], 
                constructorFunction));
        });
        
        var xField = type
            .GetFields(CppMemberBindingFlags.AnyInstance)
            .First(x => x.Name == "x");
        
        //Act
        var value = type.Create();
        
        //Assert
        
        // member access should return created instance
        xField.GetValue(value).ShouldBe(memberValue);
        constructorFunction.Received(1).Invoke(value, []);
    }


    [TestMethod]
    public void DefaultAssignment()
    {
        //Arrange
        var memberValue = CppInt32Value.Create(42);
        
        var type = new CppUserType("test");
        
        type.BuildMembers(new Scope<ICppValue>(), (builder, scope) =>
        {
            builder.AddVariable("x", CppTypes.Int32, null, MemberVisibility.Public);
            builder.AddFunction("operator=", new DefaultAssignmentOperator(type), MemberVisibility.Public);
            builder.AddConstructor(new BaseUserTypeConstructor(
                type, 
                scope, 
                [
                    ("x", new InterpreterExpressionResult(s => new CppInt32Value(5), CppTypes.Int32 ))
                ], 
                null));
        });
        
        var xField = type
            .GetFields(CppMemberBindingFlags.AnyInstance)
            .First(x => x.Name == "x");

        var assignOperator = type
            .GetFunctions(CppMemberBindingFlags.AnyInstance)
            .First(x => x.Name == "operator=");
        
        //Act
        var baseInstance = type.Create();
        var assignedInstance = type.Create();
        
        var sourceValue = xField.GetValue(assignedInstance).ShouldBeOfType<CppInt32Value>();
        sourceValue.Value = 10;
        
        assignOperator.Invoke(baseInstance, assignedInstance);
        
        //Assert
        var baseValue = xField.GetValue(baseInstance).ShouldBeOfType<CppInt32Value>();
        baseValue.Value.ShouldBe(10);
        baseValue.ShouldNotBe(sourceValue); // should be a copy
    }


    public static CppUserType CreateDummyType(string name, ICppValue memberValue)
    {
        var type = new CppUserType(name);
        
        type.BuildMembers(new Scope<ICppValue>(), (builder, scope) =>
        {
            builder.AddVariable("x", memberValue.GetCppType, null, MemberVisibility.Public);
            builder.AddFunction("operator=", new DefaultAssignmentOperator(type), MemberVisibility.Public);
            builder.AddConstructor(new BaseUserTypeConstructor(
                type, 
                scope, 
                [
                    ("x", new InterpreterExpressionResult(s => memberValue, memberValue.GetCppType ))
                ], 
                null));
        });

        return type;
    }
}