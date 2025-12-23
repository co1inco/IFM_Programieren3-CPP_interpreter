using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;


public interface ICppValueBase
{
    ICppType Type { get; }
    string StringRep();
}

public interface ICppValue : ICppValueBase
{
    static abstract ICppType SType { get; }

}

public struct CppVoidValue : ICppValue
{
    public static ICppType SType => new CppVoidType();
    public ICppType Type => SType;

    public string StringRep() => "(void)";
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
}

