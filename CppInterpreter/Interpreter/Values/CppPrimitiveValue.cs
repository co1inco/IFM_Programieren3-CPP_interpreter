using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

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