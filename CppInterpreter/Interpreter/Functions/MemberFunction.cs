using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Functions;


public sealed class MemberFunction<TInstance, TReturn>(string name, Func<TInstance, TReturn> function) : ICppFunction 
    where TInstance : ICppValueT
    where TReturn : ICppValueT
{
    public string Name => name;
    public ICppType ReturnType => TReturn.TypeOf;
    public ICppType? InstanceType => TInstance.TypeOf;
    public CppFunctionParameter[] ParameterTypes => [];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.TypeOf, instance?.GetCppType);
        
        return function(tInstance);
    }
}

public sealed class MemberFunction<TInstance, TValue1, TReturn>(string name, Func<TInstance, TValue1, TReturn> function) : ICppFunction 
    where TInstance : ICppValueT
    where TValue1 : ICppValueT
    where TReturn : ICppValueT
{
    public string Name => name;
    public ICppType ReturnType => TReturn.TypeOf;
    public ICppType? InstanceType => TInstance.TypeOf;
    public CppFunctionParameter[] ParameterTypes => [ new CppFunctionParameter("p1", TValue1.TypeOf, true) ];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.TypeOf, instance?.GetCppType);
        
        if (parameters is not [ TValue1 v1 ])
            throw new InvalidParametersException("Invalid parameters");
        
        return function(tInstance, v1);
    }
}