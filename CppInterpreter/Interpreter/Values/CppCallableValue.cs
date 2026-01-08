using CppInterpreter.Helper;
using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public class CppCallableValue : ICppValueT
{
    //TODO: remove unused scope member
    private readonly ICppValue? _instance;
    private readonly List<ICppFunction> _overloads = [];
    
    public static ICppType TypeOf => CppTypes.Callable;
    public ICppType GetCppType => new CppCallableType(Overloads.ToArray());

    public CppCallableValue(Scope<ICppValue> scope)
    {
    }

    public CppCallableValue(IEnumerable<ICppFunction> functions)
    {
        _overloads = functions.ToList();
    }
    
    public CppCallableValue(ICppValue instance, IEnumerable<ICppFunction> functions)
    {
        _instance = instance;
        _overloads = functions.ToList();
    }
    
    public string StringRep() => "<Callable>";
    public bool ToBool() => true;

    public IList<ICppFunction> Overloads => _overloads;

    public bool AddOverload(ICppFunction overload)
    {
        if (_overloads.Any(x => x.ParametersMatch(overload)))
            throw new Exception("Overloads already exists");
        
        _overloads.Add(overload);
        return true;
    }
    
    public ICppFunction? GetOverload(params IEnumerable<ICppType> parameters) =>
        _overloads.SingleOrDefault(x => x.ParametersMatch(parameters));

    public ICppValue Invoke(params ICppValue[] parameters)
    {
        if (GetOverload(parameters.Select(x => x.GetCppType)) is not {} overload)
            throw new Exception($"Overload for [{string.Join(", ", parameters.Select(x => x.GetCppType))}] doesn't exist");

        return overload.Invoke(_instance, parameters);
    }
    
    
}
