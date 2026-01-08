using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public sealed class CppInt32Value(int value) 
    : CppPrimitiveValue<int, CppInt32Value>(value)
    , ICppPrimitiveValueT<int, CppInt32Value>
{
    public static ICppType TypeOf => CppTypes.Int32;

    public static CppInt32Value Create(int value) => new CppInt32Value(value);
    public bool ToBool() => Value != 0;
}

public sealed class CppInt64ValueT(Int64 value) 
    : CppPrimitiveValue<Int64, CppInt64ValueT>(value)
    , ICppPrimitiveValueT<long, CppInt64ValueT>
{
    public static ICppType TypeOf => CppTypes.Int64;
    public static CppInt64ValueT Create(long value) => new CppInt64ValueT(value);
    public bool ToBool() => Value != 0;
}

public sealed class CppCharValueT(char value) 
    : CppPrimitiveValue<char, CppCharValueT>(value)
    , ICppPrimitiveValueT<char, CppCharValueT>
{
    public static ICppType TypeOf => CppTypes.Char;

    public static CppCharValueT Create(char value) => new CppCharValueT(value);
    public bool ToBool() => Value != '\0';
}