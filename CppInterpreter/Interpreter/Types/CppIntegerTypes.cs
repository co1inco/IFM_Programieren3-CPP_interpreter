using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public sealed class CppInt32Type : CppPrimitiveType
{
    public static ICppType Instance { get; } = new CppInt32Type();
    
    private static ICppFunction[] MemberFunctions() =>
    [
        ..CppCommonOperators.IntegerOperators<CppInt32Value, Int32>()
    ];
    
    private CppInt32Type() : base("Int32", MemberFunctions())
    {
        Constructor = [ 
            new ConstructorFunction<CppInt32Value>(() => new CppInt32Value(0) ),
            new ConstructorFunction<CppInt32Value, CppInt32Value>(i => new CppInt32Value(i.Value) ),
        ];
    }

    public override ICppValue Create() => new CppInt32Value(0);
}

public sealed class CppInt64Type : CppPrimitiveType
{
    public static ICppType Instance { get; } = new CppInt64Type();
    
    private static ICppFunction[] MemberFunctions() =>
    [
        ..CppCommonOperators.IntegerOperators<CppInt64ValueT, Int64>()
    ];
    
    private CppInt64Type() : base("Int64", MemberFunctions())
    {
        Constructor = [ 
            new ConstructorFunction<CppInt64ValueT>(() => new CppInt64ValueT(0) ),
            new ConstructorFunction<CppInt64ValueT, CppInt64ValueT>(c => new CppInt64ValueT(c.Value) ),
        ];
    }
    
    public override ICppValue Create() => new CppInt64ValueT(0);
}


public sealed class CppCharType : CppPrimitiveType
{
    public static CppCharType Instance { get; } = new CppCharType();
    
    private static ICppFunction[] MemberFunctions() =>
    [
        ..CppCommonOperators.IntegerOperators<CppCharValueT, char>()
    ];
    
    private CppCharType() : base("char", MemberFunctions())
    {
        Constructor = [ 
            new ConstructorFunction<CppCharValueT>(() => new CppCharValueT('\0') ),
            new ConstructorFunction<CppCharValueT, CppCharValueT>(i => new CppCharValueT(i.Value) ),
        ];
    }

    public override ICppValue Create() => new CppCharValueT('\0');
}