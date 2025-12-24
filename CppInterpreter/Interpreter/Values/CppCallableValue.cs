using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public class CppCallableValue : ICppValue
{
    private readonly Scope<ICppValueBase> _scope;
    private readonly List<ICppFunction> _overloads = [];
    
    public static ICppType SType => CppTypes.Callable;
    public ICppType Type => SType;

    public CppCallableValue(Scope<ICppValueBase> scope)
    {
        _scope = scope;
    }
    
    public string StringRep() => "<Callable>";

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
                .All(y => y.Right?.Equals(y.Left) ?? false));

    public ICppValueBase Invoke(params ICppValueBase[] parameters)
    {
        if (GetOverload(parameters.Select(x => x.Type)) is not {} overload)
            throw new Exception("Overload doesn't exist");

        return overload.Invoke(null, parameters);
    }
}