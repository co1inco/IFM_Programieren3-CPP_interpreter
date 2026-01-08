using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CppInterpreter.Interpreter.Functions;
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
        
        public int ExecuteFunction(string name = "main")
        {
            if (!scope.TryGetSymbol(name, out var value))
                throw new Exception($"Function '{name}' not found");

            if (value is not CppCallableValue callable)
                throw new Exception($"Symbol '{name}'is not callable");

            var result = callable.Invoke([]);

            if (result is CppInt32Value returnCode)
                return returnCode.Value;
            return 0;
        }

        public void BindFunction(ICppFunction function, string? name = null)
        {
            name ??= function.Name;

            if (scope.TryGetSymbol(name, out var value))
            {
                if (value is not CppCallableValue callable)
                    throw new Exception($"Symbol '{name}' is not callable");
                callable.AddOverload(function);
            }
            else
            {
                var callable = new CppCallableValue(scope);
                if (!scope.TryBindSymbol(name, callable))
                    throw new Exception($"Symbol '{name}' already exists");
                
                callable.AddOverload(function);
                
            }
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








