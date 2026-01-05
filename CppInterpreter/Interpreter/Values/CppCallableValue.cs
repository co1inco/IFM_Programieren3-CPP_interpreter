using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public class CppCallableValue : ICppValue
{
    private readonly Scope<ICppValueBase> _scope;
    private readonly List<ICppFunction> _overloads = [];
    
    public static ICppType TypeOf => CppTypes.Callable;
    public ICppType GetCppType => TypeOf;

    public CppCallableValue(Scope<ICppValueBase> scope)
    {
        _scope = scope;
    }
    
    public string StringRep() => "<Callable>";
    public bool ToBool() => true;

    public IList<ICppFunction> Overloads => _overloads;

    public bool AddOverload(ICppFunction overload)
    {
        if (_overloads
            .Any(x => x.ParameterTypes
                .ZipFill(overload.ParameterTypes)
                .All(y => y.Right == y.Left)))
            throw new Exception("Overloads already exists");
        
        _overloads.Add(overload);
        return true;
    }
    
    public ICppFunction? GetOverload(params IEnumerable<ICppType> parameters) =>
        _overloads
            .SingleOrDefault(x => x.ParameterTypes
                .ZipFill(parameters)
                .All(y => y.Left?.Type.Equals(y.Right) ?? false));

    public ICppValueBase Invoke(params ICppValueBase[] parameters)
    {
        if (GetOverload(parameters.Select(x => x.GetCppType)) is not {} overload)
            throw new Exception($"Overload for [{string.Join(", ", parameters.Select(x => x.GetCppType))}] doesn't exist");

        return overload.Invoke(null, parameters);
    }
    
    
}