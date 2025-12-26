using System.Diagnostics.CodeAnalysis;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter;

public class Cpp
{
    
}

public class InvalidTypeException : Exception
{
    public InvalidTypeException(ICppType expectedType, ICppType? actualType) 
        : base($"Expected '{expectedType}' but got '{actualType}'")
    {
        ExpectedType = expectedType;
        ActualType = actualType;
    }

    public ICppType ExpectedType { get; }
    public ICppType ActualType { get; }
}

public class InvalidParametersException(string message) : Exception(message)
{
    
}


public static class CppTypeExtensions
{
    extension<T>(T instance) where T : ICppValue
    {

        public ICppValueBase InvokeMemberFunc(string name, params ICppValueBase[] parameters)
        {
            // todo: look for the correct overload
            var f = T.SType.Functions.FirstOrDefault(x => x.Name == name);
            if (f is null)
                throw new Exception($"Function '{name}' not found");
            return f.Invoke(instance, parameters);
        }
        
        
    }
    
    extension(ICppType type)
    {
        public ICppFunction GetMemberFunction(string name, params ICppType[] parameters)
        {
            foreach (var function in type.Functions
                         .Where(x => x.Name == name)
                         .Where(x => x.InstanceType == type))
            {
                if (parameters is [] && function.InstanceType is null && function.ParameterTypes.Length == 0)
                    return function;

                if (parameters.ZipFill(function.ParameterTypes).All(x => x.Left?.Equals(x.Right?.Type) ?? false))
                    return function;
                
                // if (parameters is [var instance, .. var param]
                //     && instance == function.InstanceType
                //     && param.ZipFill(function.ParameterTypes).All(x => x.Left == x.Right))
                //     return function;
                //
                // if (function.InstanceType is null &&
                //     function.ParameterTypes.ZipFill(parameters).All(x => x.Left == x.Right))
                //     return function;
            }

            if (type.Functions.All(x => x.Name != name))
                throw new Exception($"Type '{type}' does not have a function named  '{name}'");
            
            throw new Exception($"No matching function '{name}' found on type '{type}'");
        }

        public bool TryGetMemberFunction(string name, [NotNullWhen(true)] out ICppFunction? function, params ICppType[] parameters)
        {
            foreach (var f in type.Functions
                         .Where(x => x.Name == name)
                         .Where(x => x.InstanceType == type))
            {
                if (parameters is [] && f.InstanceType is null && f.ParameterTypes.Length == 0)
                {
                    function = f;
                    return true;
                }
                 
                if (parameters.ZipFill(f.ParameterTypes).All(x => x.Left?.Equals(x.Right?.Type) ?? false))
                {
                    function = f;
                    return true;
                }
                
                // if (parameters is [var instance, .. var param]
                //     && instance == function.InstanceType
                //     && param.ZipFill(function.ParameterTypes).All(x => x.Left == x.Right))
                //     return function;
                //
                // if (function.InstanceType is null &&
                //     function.ParameterTypes.ZipFill(parameters).All(x => x.Left == x.Right))
                //     return function;
            }

            function = null;
            return false;
            // if (type.Functions.All(x => x.Name != name))
            //     throw new Exception($"Type '{type}' does not have a function named  '{name}'");
            //
            // throw new Exception($"No matching function '{name}' found on type '{type}'");
        }
        
        public ICppValue Construct<T>(params ICppValue[] parameters) where T : ICppValue
        {
            var parameterTypes = parameters.Select<ICppValueBase, ICppType>(x => x.Type).ToArray();
            
            var ctor = T.SType.Constructor.FirstOrDefault(x =>
                x.ParameterTypes.ZipFill(parameterTypes).All(y => y.Left == y.Right));
            
            if (ctor is null)
                throw new Exception($"Constructor '{typeof(T)}' not found");
            
            return ctor.Construct(parameters);
        }
        
        public ICppValue Construct(params ICppValueBase[] parameters)
        {
            var parameterTypes = parameters.Select<ICppValueBase, ICppType>(x => x.Type).ToArray();
            
            var ctor = type.Constructor.FirstOrDefault(x =>
                x.ParameterTypes.ZipFill(parameterTypes).All(y => y.Left == y.Right));
            
            if (ctor is null)
                throw new Exception($"Constructor '{type}' not found");
            
            return ctor.Construct(parameters);
        }
        
    }

    extension<T>(IEnumerable<T> collection)
    {
        public IEnumerable<(T? Left, TR? Right)> ZipFill<TR>(IEnumerable<TR> other)
        {
            using var l = collection.GetEnumerator();
            using var r = other.GetEnumerator();

            var lNext = l.MoveNext();
            var rNext = r.MoveNext();
            
            while (lNext && rNext)
            {
                yield return (l.Current, r.Current);
                
                lNext = l.MoveNext();
                rNext = r.MoveNext();
            }

            while (lNext)
            {
                yield return (l.Current, default(TR));
                lNext = l.MoveNext();
            }
            
            while (rNext)
            {
                yield return (default(T), r.Current);
                rNext = r.MoveNext();
            }
        }
    }

    extension(Scope<ICppValueBase> scope)
    {
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
    
    
}