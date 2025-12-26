using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public sealed class CppInt32Value(int value) 
    : CppPrimitiveValue<int, CppInt32Value>(value)
    , ICppPrimitiveValue<int, CppInt32Value>
{
    public static ICppType SType => CppTypes.Int32;

    public static CppInt32Value Create(int value) => new CppInt32Value(value);
    public bool ToBool() => Value != 0;
}

public sealed class CppInt64Value(Int64 value) 
    : CppPrimitiveValue<Int64, CppInt64Value>(value)
    , ICppPrimitiveValue<long, CppInt64Value>
{
    public static ICppType SType => CppTypes.Int64;
    public static CppInt64Value Create(long value) => new CppInt64Value(value);
    public bool ToBool() => Value != 0;
}

public sealed class CppCharValue(char value) 
    : CppPrimitiveValue<char, CppCharValue>(value)
    , ICppPrimitiveValue<char, CppCharValue>
{
    public static ICppType SType => CppTypes.Char;

    public static CppCharValue Create(char value) => new CppCharValue(value);
    public bool ToBool() => Value != '\0';
}