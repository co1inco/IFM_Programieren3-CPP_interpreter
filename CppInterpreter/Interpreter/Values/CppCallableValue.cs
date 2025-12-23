using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public class CppCallableValue : ICppValue
{
    private readonly Scope<ICppValueBase> _scope;
    private List<ICppFunction> _overloads = [];
    
    public static ICppType SType => CppTypes.Callable;
    public ICppType Type => SType;

    public CppCallableValue(Scope<ICppValueBase> scope)
    {
        _scope = scope;
    }
    
    public string StringRep() => "<Callable>";

    public ICollection<ICppFunction> Overloads => _overloads;

    public bool AddOverload(ICppFunction overload)
    {
        throw new NotImplementedException();
    }
    
    public ICppFunction? GetOverload(params ICppType[] parameters)
    {
        throw new NotImplementedException();
    }
}