using System.Numerics;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

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

    public static ICppType Char => field ??= new CppCharType();
    public static ICppType Int32 => field ??= new CppInt32Type();
    public static ICppType Int64 => field ??= new CppInt64Type();
    
    public static ICppType Void => field ??= new CppVoidType();
    public static ICppType Boolean => field ??= new CppBoolType();
    public static ICppType String => field ??= new CppStringType();
    
    public static ICppType Callable => field ??= new CppCallableType();
    
}


public interface ICppType : IEquatable<ICppType>
{
    string Name { get; }   

    ICppConstructor[] Constructor { get; }
    ICppFunction[] Functions { get; }
    ICppConverter[] Converter { get; }

    public bool IsAssignableTo(ICppType other);

    ICppValueBase Create();
}


public abstract class CppPrimitiveType : ICppType
{
    protected CppPrimitiveType(string name, ICppFunction[]? functions = null)
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

    public abstract ICppValueBase Create();
}

public sealed class CppVoidType : CppPrimitiveType
{
    public CppVoidType() : base("void")
    {
        Constructor = [ new ConstructorFunction<CppVoidValue>(() => new CppVoidValue() ) ];
        Functions = [];
    }

    public override ICppValueBase Create() => new CppVoidValue();
};


public sealed class CppBoolType : CppPrimitiveType
{
    public CppBoolType() : base("bool")
    {
        Functions =
        [
            ..CppCommonOperators.EquatorOperators<CppBoolValue, bool>(),
            CppCommonOperators.PrimitiveAssignment<CppBoolValue, bool>(),
            // new MemberFunction<CppBoolValue, CppBoolValue, CppBoolValue>("operator&&", 
            //     (a, b) => new CppBoolValue(a.Value && b.Value)),
            // new MemberFunction<CppBoolValue, CppBoolValue, CppBoolValue>("operator||", 
            //     (a, b) => new CppBoolValue(a.Value || b.Value))
        ];
    }

    public override ICppValueBase Create() => new CppBoolValue(false);
}



