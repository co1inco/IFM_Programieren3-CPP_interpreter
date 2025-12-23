using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public sealed class CppInt32Type : CppPrimitiveType
{
    public CppInt32Type() : base("Int32")
    {
        Constructor = [ new ConstructorFunction<CppInt32Value>(() => new CppInt32Value(0) ) ];
        Functions =
        [
            ..CppCommonOperators.IntegerOperators<CppInt32Value, Int32>()
        ];
    }

    public override ICppValueBase Create() => new CppInt32Value(0);
}

public sealed class CppInt64Type : CppPrimitiveType
{
    public CppInt64Type() : base("Int64")
    {
        Constructor = [ new ConstructorFunction<CppInt64Value>(() => new CppInt64Value(0) ) ];
        Functions =
        [
            ..CppCommonOperators.IntegerOperators<CppInt64Value, Int64>()
        ];
    }
    
    public override ICppValueBase Create() => new CppInt64Value(0);
}
