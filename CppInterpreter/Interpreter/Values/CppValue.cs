using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

[Flags]
public enum CppMemberBindingFlags
{
    Public = 1,
    NonPublic = 2,
    Static = 4,
    Instance = 8,
    PublicInstance = 1 | 8
}

public interface ICppValue
{
    ICppType GetCppType { get; }
    string StringRep();
    
    bool ToBool();
}

/// <summary>
/// <see cref="ICppValue"/> that statically knows its cpp type.
/// Similar to using typeof(int)
/// </summary>
public interface ICppValueT : ICppValue
{
    static abstract ICppType TypeOf { get; }

}

public static class CppValues
{
    // private static void Test()
    // {
    //     typeof(int).GetMembers()
    //     
    //     BindingFlags
    // }
    
    extension(Scope<ICppValue> scope)
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
    
    extension<T>(T instance) where T : ICppValueT
    {

        public ICppValue InvokeMemberFunc(string name, params ICppValue[] parameters)
        {
            // todo: look for the correct overload
            var f = T.TypeOf.Functions.FirstOrDefault(x => x.Name == name);
            if (f is null)
                throw new Exception($"Function '{name}' not found");
            return f.Invoke(instance, parameters);
        }
        
        
    }
}


public struct CppVoidValueT : ICppValueT
{
    public static ICppType TypeOf => CppTypes.Void;
    public ICppType GetCppType => TypeOf;

    public string StringRep() => "(void)";
    public bool ToBool() => false;
}


public interface ICppPrimitiveValueT<T, out TType> : ICppValueT
{
    public static abstract TType Create(T value);
    public T Value { get; set; }
};


public abstract class CppPrimitiveValue<T, TType>(T value) where TType : ICppValueT
{
    public ICppType GetCppType => TType.TypeOf;

    public T Value { get; set; } = value;

    public override string ToString() => Value?.ToString() ?? "null";
    
    public string StringRep() => Value?.ToString() ?? "(null)";
}

public sealed class CppBoolValueT(bool value)
    : CppPrimitiveValue<bool, CppBoolValueT>(value)
    , ICppPrimitiveValueT<bool, CppBoolValueT>
{
    public static ICppType TypeOf => CppTypes.Boolean;
    public static CppBoolValueT Create(bool value) => new CppBoolValueT(value);
    public bool ToBool() => Value;
}

