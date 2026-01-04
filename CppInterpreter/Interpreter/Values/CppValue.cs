using System.Diagnostics.CodeAnalysis;
using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;


public interface ICppValueBase
{
    ICppType Type { get; }
    string StringRep();
    
    bool ToBool();
}

public interface ICppValue : ICppValueBase
{
    static abstract ICppType SType { get; }

}

public static class CppValues
{
    extension(Scope<ICppValueBase> scope)
    {
        public bool TryBindFunction(string name, ICppFunction func)
        {

            if (!scope.TryEnsureCallable(name, out var callable))
                return false;

            callable.AddOverload(func);
            return true;
        }

        private bool TryEnsureCallable(string name, [NotNullWhen(true)] out CppCallableValue? callable)
        {
            if (!scope.TryGetSymbol(name, out var symbol))
            {
                callable = new CppCallableValue(scope);
                return scope.TryBindSymbol(name, callable);
            }

            if (symbol is not CppCallableValue c)
            {
                callable = null;
                return false;
            }

            callable = c;
            return true;
        } 
        
        
    }
}


public struct CppVoidValue : ICppValue
{
    public static ICppType SType => CppTypes.Void;
    public ICppType Type => SType;

    public string StringRep() => "(void)";
    public bool ToBool() => false;
}


public interface ICppPrimitiveValue<T, out TType> : ICppValue
{
    public static abstract TType Create(T value);
    public T Value { get; set; }
};


public abstract class CppPrimitiveValue<T, TType>(T value) where TType : ICppValue
{
    public ICppType Type => TType.SType;

    public T Value { get; set; } = value;

    public override string ToString() => Value?.ToString() ?? "null";
    
    public string StringRep() => Value?.ToString() ?? "(null)";
}

public sealed class CppBoolValue(bool value)
    : CppPrimitiveValue<bool, CppBoolValue>(value)
    , ICppPrimitiveValue<bool, CppBoolValue>
{
    public static ICppType SType => CppTypes.Boolean;
    public static CppBoolValue Create(bool value) => new CppBoolValue(value);
    public bool ToBool() => Value;
}

