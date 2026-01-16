using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Functions;

public sealed class CppFunction<TReturn>(string name, Func<TReturn> function, ICppType returnType) : ICppFunction
    where TReturn : ICppValue
{
    public string Name => name;
    public ICppType ReturnType => returnType;
    public ICppType? InstanceType => null;
    public CppFunctionParameter[] ParameterTypes => [];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not null)
            throw new Exception("Function is not a member function");

        return function();
    }
}

public sealed class CppFunctionP<TReturn>(string name, Func<ICppValue[], TReturn> function, ICppType returnType, CppFunctionParameter[] parameterTypes) : ICppFunction
    where TReturn : ICppValue
{
    public string Name => name;
    public ICppType ReturnType => returnType;
    public ICppType? InstanceType => null;
    public CppFunctionParameter[] ParameterTypes => parameterTypes;
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not null)
            throw new Exception("Function is not a member function");

        if (parameters.Length != parameterTypes.Length)
            throw new Exception("Invalid parameter count");
        
        if (parameters.Zip(ParameterTypes).Any(x => !(x.First.GetCppType?.Equals(x.Second.Type) ?? false)))
            throw new Exception("Parameter mismatch");
        
        return function(parameters);
    }
}