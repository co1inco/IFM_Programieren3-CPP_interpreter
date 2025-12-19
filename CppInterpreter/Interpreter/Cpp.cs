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



public interface ICppValue
{
    static abstract ICppType SType { get; }
    
    ICppType Type { get; }
}

public struct CppVoidValue : ICppValue
{
    public static ICppType SType => new CppVoidType();
    public ICppType Type => SType;
}


public interface ICppPrimitiveValue<T, TType> : ICppValue
{
    public static abstract TType Create(T value);
    public T Value { get; set; }
};


public abstract class CppPrimitiveValue<T, TType>(T value) where TType : ICppValue
{
    public ICppType Type => TType.SType;

    public T Value { get; set; } = value;

    public override string ToString() => Value?.ToString() ?? "null";
}

public sealed class CppInt32Value(int value) 
    : CppPrimitiveValue<int, CppInt32Value>(value)
    , ICppPrimitiveValue<int, CppInt32Value>
{
    public static ICppType SType => CppTypes.Int32;
    public static CppInt32Value Create(int value) => new CppInt32Value(value);
}

public sealed class CppInt64Value(Int64 value) 
    : CppPrimitiveValue<Int64, CppInt64Value>(value)
    , ICppPrimitiveValue<long, CppInt64Value>
{
    public static ICppType SType => CppTypes.Int64;
    public static CppInt64Value Create(long value) => new CppInt64Value(value);
}

public static class CppTypeExtensions
{
    extension<T>(T instance) where T : ICppValue
    {

        public ICppValue InvokeMemberFunc(string name, params ICppValue[] parameters)
        {
            // todo: look for the correct overload
            var f = T.SType.Functions.FirstOrDefault(x => x.Name == name);
            if (f is null)
                throw new Exception($"Function '{name}' not found");
            return f.Invoke(instance, parameters);
        }
        
        
    }
    
    
}