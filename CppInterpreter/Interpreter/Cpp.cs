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
        public ICppFunction GetFunction(string name, params ICppType[] parameters)
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
    
}