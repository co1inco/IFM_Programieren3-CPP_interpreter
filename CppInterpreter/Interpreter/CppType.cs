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
}


public interface ICppType
{
    string Name { get; }   

    ICppFunction[] Functions { get; }
    ICppConverter[] Converter { get; }
}

public readonly struct CppVoidType : ICppType
{
    public CppVoidType() { }

    public string Name => "void";
    public ICppFunction[] Functions { get; } = [];

    public ICppConverter[] Converter { get; } = [];

}


public abstract class CppPrimitiveBaseType<T> : ICppType where T : INumber<T>
{
    public abstract string Name { get; }
    public abstract ICppFunction[] Functions { get; }
    public abstract ICppConverter[] Converter { get; }
}

public sealed class CppInt32Type : ICppType
{
    public string Name => "int";

    public ICppFunction[] Functions { get; } =
    [
        new MemberFunction<CppInt32Value, CppInt32Value, CppInt32Value>(
            "operator+",
            (a, b) => new CppInt32Value(a.Value + b.Value)),
        new MemberFunction<CppInt32Value, CppInt32Value, CppInt32Value>(
            "operator-",
            (a, b) => new CppInt32Value(a.Value - b.Value)),
        new MemberFunction<CppInt32Value, CppInt32Value, CppInt32Value>(
            "operator*",
            (a, b) => new CppInt32Value(a.Value * b.Value)),
        new MemberFunction<CppInt32Value, CppInt32Value, CppInt32Value>(
            "operator/",
            (a, b) => new CppInt32Value(a.Value / b.Value)),
        new MemberFunction<CppInt32Value, CppInt32Value, CppInt32Value>(
            "operator%",
            (a, b) => new CppInt32Value(a.Value % b.Value)),
    ];

    public ICppConverter[] Converter { get; } = [];
} 



