using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public interface ICppPrimitiveValueT<T, out TType> : ICppValueT
{
    public static abstract TType Create(T value);
    public T Value { get; set; }
};


public abstract class CppPrimitiveValue<T, TType>(T value)
    where TType : ICppValueT, ICppPrimitiveValueT<T, TType>
{
    public ICppType GetCppType => TType.TypeOf;

    public T Value { get; set; } = value;

    public override string ToString() => Value?.ToString() ?? "null";
    
    public string StringRep() => Value?.ToString() ?? "(null)";
    
    public ICppValue Copy() => TType.Create(Value);

    public void Assign(ICppValue value)
    {
        if (value is not ICppPrimitiveValueT<T, TType> c)
            throw new Exception($"Can not assign '{value.GetCppType.Name}' to '{GetCppType.Name}'");
        Value = c.Value;
    }
    
    public Scope<ICppValue> InstanceScope { get; } = new();
    
}