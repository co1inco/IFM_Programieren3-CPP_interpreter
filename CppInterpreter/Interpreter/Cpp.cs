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


public class CppPrimitiveValue<T, TType>(T value) : ICppValue where TType : ICppType, new()
{
    public static ICppType SType { get; } = CppTypes.GetType<TType>();
    public ICppType Type => SType;

    public T Value { get; set; } = value;

    public override string ToString() => Value?.ToString() ?? "null";
}

public sealed class CppInt32Value(int value) : CppPrimitiveValue<int, CppInt32Type>(value); 
public sealed class CppInt64Value(Int64 value) : CppPrimitiveValue<Int64, CppInt32Type>(value); 


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