using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;
using CSharpFunctionalExtensions;

namespace CppInterpreter.Interpreter.Values;

[Flags]
public enum CppMemberBindingFlags
{
    Public = 1,
    NonPublic = 2,
    Static = 4,
    Instance = 8,
    PublicInstance = 1 | 8,
    AnyInstance = 1 | 2 | 8
}

public interface ICppValue
{
    ICppType GetCppType { get; }
    string StringRep();
    
    bool ToBool();

    ICppValue Copy();
    
    Scope<ICppValue> InstanceScope { get; }
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
        public Result TryBindFunction(string name, ICppFunction func)
        {
            return scope.TryEnsureCallable(name)
                .Ensure(c => c.TryAddOverload(func), "Ovlerload already exists");
        }

        private Result<CppCallableValue> TryEnsureCallable(string name)
        {
            if (scope.TryGetSymbol(name, out var symbol))
            {
                return symbol is CppCallableValue c
                    ? Result.Success(c)
                    : Result.Failure<CppCallableValue>($"Symbol '{name}' is not callable");
            }
            
            var callable = new CppCallableValue(scope);
            return Result.SuccessIf(
                scope.TryBindSymbol(name, callable),
                callable,
                $"Failed to bind callable '{name}'"
            );
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

        public bool BindFunction(ICppFunction function, string? name = null)
        {
            name ??= function.Name;

            if (scope.TryGetSymbol(name, out var value))
            {
                if (value is not CppCallableValue callable)
                    throw new Exception($"Symbol '{name}' is not callable");
                return callable.TryAddOverload(function);
            }
            else
            {
                var callable = new CppCallableValue(scope);
                if (!scope.TryBindSymbol(name, callable))
                    throw new Exception($"Symbol '{name}' already exists");
                
                return callable.TryAddOverload(function);
            }
        }
        
    }
    
    extension<T>(T instance) where T : ICppValueT
    {

        public ICppValue InvokeMemberFunc(string name, params ICppValue[] parameters)
        {
            // todo: look for the correct overload
            var f = T.TypeOf.GetFunction(name, CppMemberBindingFlags.AnyInstance);
            if (f is null)
                throw new Exception($"Function '{name}' not found");
            return f.Invoke(instance, parameters);
        }
    }
}








