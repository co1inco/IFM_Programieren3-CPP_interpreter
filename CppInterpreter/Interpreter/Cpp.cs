using System.Diagnostics.CodeAnalysis;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter;


public class InvalidTypeException : Exception
{
    public InvalidTypeException(ICppType expectedType, ICppType? actualType) 
        : base($"Expected '{expectedType}' but got '{actualType}'")
    {
        ExpectedType = expectedType;
        ActualType = actualType;
    }

    public ICppType ExpectedType { get; }
    public ICppType ActualType { get; }
}

public class InvalidParametersException(string message) : Exception(message)
{
    
}


public static class CppTypeExtensions
{
    


    extension(Scope<ICppValue> scope)
    {
        public int ExecuteFunction(string name = "main")
        {
            if (!scope.TryGetSymbol(name, out var value))
                throw new Exception($"Function '{name}' not found");

            if (value is not CppCallableValue callable)
                throw new Exception($"Symbol '{name}'is not callable");

            var result = callable.Invoke([]);

            if (result is CppInt32Value returnCode)
                return returnCode.Value;
            return 0;
        }

        public void BindFunction(ICppFunction function, string? name = null)
        {
            name ??= function.Name;

            if (scope.TryGetSymbol(name, out var value))
            {
                if (value is not CppCallableValue callable)
                    throw new Exception($"Symbol '{name}' is not callable");
                callable.AddOverload(function);
            }
            else
            {
                var callable = new CppCallableValue(scope);
                if (!scope.TryBindSymbol(name, callable))
                    throw new Exception($"Symbol '{name}' already exists");
                
                callable.AddOverload(function);
                
            }
        }
        
    }
    
    
}