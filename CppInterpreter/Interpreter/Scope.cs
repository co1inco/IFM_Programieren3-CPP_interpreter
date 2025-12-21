using System.Diagnostics.CodeAnalysis;

namespace CppInterpreter.Interpreter;

public class Scope<T>
{
    private readonly Dictionary<string, T> _symbols = [];
    private readonly Scope<T>? _parentScope;
    
    public Scope()
    {
        
    }

    public Scope(Scope<T>? parentScope)
    {
        _parentScope = parentScope;
    }

    public Scope<T> ChildScope() => new Scope<T>(this);

    public bool TryGetSymbolLocal(string name, [NotNullWhen(true)] out T? value) =>
        _symbols.TryGetValue(name, out value);
    
    public bool TryGetSymbol(string name, [NotNullWhen(true)] out T? value)
    {
        if (TryGetSymbolLocal(name, out value))
            return true;

        if (_parentScope is not null)
            _parentScope.TryGetSymbol(name, out value);

        value = default(T);
        return false;
    }
    
    public bool HasSymbol(string name)
    {
        if (_symbols.ContainsKey(name))
            return true;
        
        return _parentScope?.HasSymbol(name) ?? false;
    }
    
    public bool HasSymbolLocal(string name) => 
        _symbols.ContainsKey(name);

    // public void AddSymbol(string name, T symbol)
    // {
    //     _symbols[name] = symbol;
    // }

    public bool TryAddSymbol(string name, T symbol) => 
        _symbols.TryAdd(name, symbol);
    
    public bool TryBindSymbol(string name, T symbol) => 
        TryAddSymbol(name, symbol);
    
}