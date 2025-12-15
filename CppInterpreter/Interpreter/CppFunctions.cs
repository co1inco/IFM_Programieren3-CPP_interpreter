namespace CppInterpreter.Interpreter;

public interface ICppFunction
{
    string Name { get; }
    ICppType ReturnType { get; }
    ICppType? InstanceType { get; }
    ICppType[] ParameterTypes { get; }

    ICppValue Invoke(ICppValue? instance, ICppValue[] parameters);
}

public sealed class MemberAction<TInstance>(string name, Action<TInstance> action) : ICppFunction 
    where TInstance : ICppValue
{
    public string Name => name;
    public ICppType ReturnType => new CppVoidType();
    public ICppType? InstanceType => TInstance.SType;
    public ICppType[] ParameterTypes => [];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.SType, instance?.Type);
        action(tInstance);
        
        return new CppVoidValue();
    }
}

public sealed class MemberAction<TInstance, TValue1>(string name, Action<TInstance, TValue1> action) : ICppFunction 
    where TInstance : ICppValue
    where TValue1 : ICppValue
{
    public string Name => name;
    public ICppType ReturnType => new CppVoidType();
    public ICppType? InstanceType => TInstance.SType;
    public ICppType[] ParameterTypes => [];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.SType, instance?.Type);
        
        if (parameters is not [ TValue1 v1 ])
            throw new InvalidParametersException("Invalid parameters");
        
        action(tInstance, v1);
        
        return new CppVoidValue();
    }
}

public sealed class MemberFunction<TInstance, TReturn>(string name, Func<TInstance, TReturn> function) : ICppFunction 
    where TInstance : ICppValue
    where TReturn : ICppValue
{
    public string Name => name;
    public ICppType ReturnType => new CppVoidType();
    public ICppType? InstanceType => TInstance.SType;
    public ICppType[] ParameterTypes => [];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.SType, instance?.Type);
        
        return function(tInstance);
    }
}

public sealed class MemberFunction<TInstance, TValue1, TReturn>(string name, Func<TInstance, TValue1, TReturn> function) : ICppFunction 
    where TInstance : ICppValue
    where TValue1 : ICppValue
    where TReturn : ICppValue
{
    public string Name => name;
    public ICppType ReturnType => new CppVoidType();
    public ICppType? InstanceType => TInstance.SType;
    public ICppType[] ParameterTypes => [];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.SType, instance?.Type);
        
        if (parameters is not [ TValue1 v1 ])
            throw new InvalidParametersException("Invalid parameters");
        
        return function(tInstance, v1);
    }
}