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
    
    public virtual bool TryGetSymbol(string name, [NotNullWhen(true)] out T? value)
    {
        if (TryGetSymbolLocal(name, out value))
            return true;

        if (_parentScope is not null)
            return _parentScope.TryGetSymbol(name, out value);

        value = default(T);
        return false;
    }
    
    public virtual bool HasSymbol(string name)
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

//TODO: Don't inherit scope. Instead create a new IScope interface and replace all usage of Scope with IScope
public class IntermediateScope<T> : Scope<T>
{
    private readonly Scope<T> _intermediateScope;

    public IntermediateScope(Scope<T>? parentScope, Scope<T> intermediateScope) :base(parentScope)
    {
        _intermediateScope = intermediateScope;
    }

    public override bool TryGetSymbol(string name, [NotNullWhen(true)] out T? value)
    {
        if (TryGetSymbolLocal(name, out value))
            return true;
        if (_intermediateScope.TryGetSymbolLocal(name, out value))
            return true;
        return base.TryGetSymbol(name, out value);
    }

    public override bool HasSymbol(string name)
    {
        if (HasSymbolLocal(name))
            return true;
        if (_intermediateScope.HasSymbolLocal(name))
            return true;
        return base.HasSymbol(name);
    }
}