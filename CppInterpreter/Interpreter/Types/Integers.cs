using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public sealed class CppInt32Type : CppPrimitiveType
{
    public CppInt32Type() : base("Int32")
    {
        Constructor = [ 
            new ConstructorFunction<CppInt32Value>(() => new CppInt32Value(0) ),
            new ConstructorFunction<CppInt32Value, CppInt32Value>(i => new CppInt32Value(i.Value) ),
        ];
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
        Constructor = [ 
            new ConstructorFunction<CppInt64Value>(() => new CppInt64Value(0) ),
            new ConstructorFunction<CppInt64Value, CppInt64Value>(c => new CppInt64Value(c.Value) ),
        ];
        Functions =
        [
            ..CppCommonOperators.IntegerOperators<CppInt64Value, Int64>()
        ];
    }
    
    public override ICppValueBase Create() => new CppInt64Value(0);
}


public sealed class CppCharType : CppPrimitiveType
{
    public CppCharType() : base("char")
    {
        Constructor = [ 
            new ConstructorFunction<CppCharValue>(() => new CppCharValue('\0') ),
            new ConstructorFunction<CppCharValue, CppCharValue>(i => new CppCharValue(i.Value) ),
        ];
        Functions =
        [
            ..CppCommonOperators.IntegerOperators<CppCharValue, char>()
        ];
    }

    public override ICppValueBase Create() => new CppCharValue('\0');
}