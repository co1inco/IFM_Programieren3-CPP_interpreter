using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Values;
using Shouldly;

namespace CppInterpreter.Test.Interpreter;

[TestClass]
public class PrimitiveFunctions
{


    [TestMethod]
    public void Assignment()
    {
        //Arrange
        var instance = new CppInt32Value(31);
        var value = new CppInt32Value(2);

        //Act
        instance.InvokeMemberFunc("operator=", value);

        //Assert
        instance.Value.ShouldBe(2);
    }
    
}