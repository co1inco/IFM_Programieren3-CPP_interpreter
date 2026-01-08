using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Functions;



public sealed class MemberAction<TInstance>(string name, Action<TInstance> action) : ICppFunction 
    where TInstance : ICppValueT
{
    public string Name => name;
    public ICppType ReturnType => CppTypes.Void;
    public ICppType? InstanceType => TInstance.TypeOf;
    public CppFunctionParameter[] ParameterTypes => [];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.TypeOf, instance?.GetCppType);
        action(tInstance);
        
        return new CppVoidValue();
    }
}

public sealed class MemberAction<TInstance, TValue1>(string name, Action<TInstance, TValue1> action) : ICppFunction 
    where TInstance : ICppValueT
    where TValue1 : ICppValueT
{
    public string Name => name;
    public ICppType ReturnType => CppTypes.Void;
    public ICppType? InstanceType => TInstance.TypeOf;
    public CppFunctionParameter[] ParameterTypes => [ new CppFunctionParameter("p1", TValue1.TypeOf, true) ];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.TypeOf, instance?.GetCppType);
        
        if (parameters is not [ TValue1 v1 ])
            throw new InvalidParametersException("Invalid parameters");
        
        action(tInstance, v1);
        
        return new CppVoidValue();
    }
}