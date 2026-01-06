using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Values;
using Shouldly;

namespace CppInterpreter.Test.Interpreter;

[TestClass]
public class FunctionInvocation
{

    [TestMethod]
    public void Int32_Add()
    {
        //Arrange
        var a = new CppInt32Value(1);
        var b = new CppInt32Value(2);

        //Act
        var c = a.InvokeMemberFunc("operator+", b);
        
        //Assert
        c.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(3);
    }
    
    [TestMethod]
    public void Int32_Sub()
    {
        //Arrange
        var a = new CppInt32Value(1);
        var b = new CppInt32Value(2);

        //Act
        var c = a.InvokeMemberFunc("operator-", b);
        
        //Assert
        c.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(-1);
    }
    
    [TestMethod]
    public void Int32_Mul()
    {
        //Arrange
        var a = new CppInt32Value(2);
        var b = new CppInt32Value(3);

        //Act
        var c = a.InvokeMemberFunc("operator*", b);
        
        //Assert
        c.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(6);
    }
    
    [TestMethod]
    public void Int32_Div()
    {
        //Arrange
        var a = new CppInt32Value(10);
        var b = new CppInt32Value(5);

        //Act
        var c = a.InvokeMemberFunc("operator/", b);
        
        //Assert
        c.ShouldBeOfType<CppInt32Value>().Value.ShouldBe(2);
    }
    
    [TestMethod]
    public void Int64_Add()
    {
        //Arrange
        var a = new CppInt64ValueT(1);
        var b = new CppInt64ValueT(2);

        //Act
        var c = a.InvokeMemberFunc("operator+", b);
        
        //Assert
        c.ShouldBeOfType<CppInt64ValueT>().Value.ShouldBe(3);
    }
    
    [TestMethod]
    public void Int64_Sub()
    {
        //Arrange
        var a = new CppInt64ValueT(1);
        var b = new CppInt64ValueT(2);

        //Act
        var c = a.InvokeMemberFunc("operator-", b);
        
        //Assert
        c.ShouldBeOfType<CppInt64ValueT>().Value.ShouldBe(-1);
    }
    
    [TestMethod]
    public void Int64_Mul()
    {
        //Arrange
        var a = new CppInt64ValueT(2);
        var b = new CppInt64ValueT(3);

        //Act
        var c = a.InvokeMemberFunc("operator*", b);
        
        //Assert
        c.ShouldBeOfType<CppInt64ValueT>().Value.ShouldBe(6);
    }
    
    [TestMethod]
    public void Int64_Div()
    {
        //Arrange
        var a = new CppInt64ValueT(10);
        var b = new CppInt64ValueT(5);

        //Act
        var c = a.InvokeMemberFunc("operator/", b);
        
        //Assert
        c.ShouldBeOfType<CppInt64ValueT>().Value.ShouldBe(2);
    }
}