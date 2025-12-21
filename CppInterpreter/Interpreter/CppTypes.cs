using System.Numerics;

namespace CppInterpreter.Interpreter;

public class CppTypes
{
    private static readonly Dictionary<Type, ICppType> _types = [];


    public static T GetType<T>() where T : ICppType, new()
    {
        if (_types.TryGetValue(typeof(T), out var type))
            return (T)type;

        var t = new T();
        _types.Add(typeof(T), t);
        return t;
    } 
    
    public static ICppType GetType(Type type)
    {
        if (!type.IsAssignableTo(typeof(ICppType)))
            throw new Exception($"Type '{type}' is not assignable to ICppType");
        
        if (_types.TryGetValue(type, out var instance))
            return instance;

        var t = (ICppType)Activator.CreateInstance(type)!;
        _types.Add(type, t);
        return t;
    }

    public static ICppType Int32 => field ??= new CppInt32Type();
    public static ICppType Int64 => field ??= new CppInt64Type();
    
    public static ICppType Void => field ??= new CppVoidType();
    
    
}


public interface ICppType : IEquatable<ICppType>
{
    string Name { get; }   

    ICppConstructor[] Constructor { get; }
    ICppFunction[] Functions { get; }
    ICppConverter[] Converter { get; }

    public bool IsAssignableTo(ICppType other);
}


public class CppPrimitiveType : ICppType
{
    public CppPrimitiveType(string name, ICppFunction[]? functions = null)
    {
        Name = name;
        if (functions is not null)
            Functions = functions;
    }
    
    public string Name { get; }

    public ICppConstructor[] Constructor { get; init; } = [];
    public ICppFunction[] Functions { get; init; } = [];
    public ICppConverter[] Converter { get; init; } = [];
    
    public bool IsAssignableTo(ICppType other) => other.GetType() == GetType();
    
    public bool Equals(ICppType? other) => Name == other?.Name;
    
}

public sealed class CppVoidType : CppPrimitiveType
{
    public CppVoidType() : base("void")
    {
        Constructor = [ new ConstructorFunction<CppVoidValue>(() => new CppVoidValue() ) ];
        Functions = [];
    }
};

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
}


public static class CppCommonOperators
{



    public static IEnumerable<ICppFunction> ArithmeticOperators<T, TP>()
        where T : ICppPrimitiveValue<TP, T>
        where TP : INumber<TP> =>
    [
        new MemberFunction<T, T, T>("operator+", (a, b) => T.Create(a.Value + b.Value)),
        new MemberFunction<T, T, T>("operator-", (a, b) => T.Create(a.Value - b.Value)),
        new MemberFunction<T, T, T>("operator*", (a, b) => T.Create(a.Value * b.Value)),
        new MemberFunction<T, T, T>("operator/", (a, b) => T.Create(a.Value / b.Value)),
        new MemberFunction<T, T, T>("operator%", (a, b) => T.Create(a.Value % b.Value)),
    ];

    public static ICppFunction PrimitiveAssignment<T, TP>() where T : ICppPrimitiveValue<TP, T> => 
        new MemberAction<T, T>("operator=", (i, a) => i.Value = a.Value);
    
    public static IEnumerable<ICppFunction> IntegerOperators<T, TP>()
        where T : ICppPrimitiveValue<TP, T>
        where TP : INumber<TP> =>
    [
        ..ArithmeticOperators<T, TP>(),
        PrimitiveAssignment<T, TP>()
    ];
}
