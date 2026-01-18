using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using Shouldly;

namespace CppInterpreter.Test.Interpreter.UserType;

[TestClass]
public class UserFunctionTest
{


    [TestMethod]
    public void ScopeUsage()
    {
        //Arrange

        var memberInstance = new CppInt32Value(42);
        var type = UserTypeTest.CreateDummyType("test", memberInstance);
        
        var function = CreateUserFunction(
            "test",
            CppTypes.Void,
            type,
            s =>
            {
                s.TryGetSymbol("x", out var v).ShouldBeTrue();
                v.ShouldBe(memberInstance);

                return new CppVoidValue();
            });

        var instance = type.Create();
        
        //Act
        function.Invoke(instance, []);
        
        //Assert
    }
    
    
    
    public static CppUserFunction CreateUserFunction(
        string name,
        ICppType returnType,
        ICppType instanceType,
        Func<Scope<ICppValue>, ICppValue> functionBody)
    {
        var f = new CppUserFunction("", returnType, instanceType, [], new AstBlock([], AstMetadata.Generated()));
        
        f.BuildBody(new Scope<ICppValue>(), new Scope<ICppType>(), (block, type, s1, s2) => functionBody);

        return f;
    }
}