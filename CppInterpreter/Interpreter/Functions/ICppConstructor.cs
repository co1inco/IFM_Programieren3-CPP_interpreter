using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Functions;


public interface ICppConstructor
{
    ICppType InstanceType { get; }
    ICppType[] ParameterTypes { get; }
    ICppValueT Construct(ICppValue[] parameters);
};

public sealed class ConstructorFunction<TInstance>(Func<TInstance> constructor) : ICppConstructor
    where TInstance : ICppValueT
{
    public ICppType InstanceType => TInstance.TypeOf;
    public ICppType[] ParameterTypes => [ ];

    public ICppValueT Construct(ICppValue[] parameters) => constructor();
}

public sealed class ConstructorFunction<TInstance, TValue1>(Func<TValue1, TInstance> constructor, bool @explicit = false) : ICppConstructor
    where TInstance : ICppValueT
    where TValue1 : ICppValueT
{
    public ICppType InstanceType => TInstance.TypeOf;
    public ICppType[] ParameterTypes => [ TValue1.TypeOf ];
    public ICppValueT Construct(ICppValue[] parameters)
    {
        if (parameters is not [ TValue1 v1 ])
            throw new InvalidParametersException("Invalid parameters");
        
        return constructor(v1);
    }

    /// <summary>
    /// Indicates that a function should not be used for implicit conversions.
    /// Only relevant for single parameter constructors
    /// (currently not used)
    /// </summary>
    public bool Explicit => @explicit;
}