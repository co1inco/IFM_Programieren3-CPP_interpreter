using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
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

    public static ICppType Char => CppCharType.Instance;
    public static ICppType Int32 => CppInt32Type.Instance;
    public static ICppType Int64 => CppInt64Type.Instance;
    
    public static ICppType Void => CppVoidType.Instance;
    public static ICppType Boolean => CppBoolType.Instance;
    public static ICppType String => CppStringType.Instance;
    
    public static ICppType Callable => field ??= new CppCallableType();
    
}

public interface ICppMemberInfo
{
    string Name { get; }
    ICppType MemberType { get; }
    ICppValue GetValue(ICppValue instance);
}

public record CppMethodInfo(string Name, CppCallableType Callable);

public interface ICppType : IEquatable<ICppType>
{
    string Name { get; }   

    ICppConstructor[] Constructor { get; }
    ICppFunction[] Functions { get; }
    ICppConverter[] Converter { get; }

    public bool IsAssignableTo(ICppType other);

    ICppValue Create();

    IEnumerable<ICppMemberInfo> GetMembers(CppMemberBindingFlags flags);

    ICppMemberInfo? GetMember(string name, CppMemberBindingFlags flags) => 
        GetMembers(flags)
            .FirstOrDefault(m => m.Name == name);

    // MethodInfo useful for implementing the interpreter?
    IEnumerable<CppMemberFunctionInfo> GetFunctions(CppMemberBindingFlags flags);
    // IEnumerable<CppMethodInfo> GetFunctions(CppMemberBindingFlags flags) => throw new NotImplementedException();
    CppMemberFunctionInfo? GetFunction(string name, CppMemberBindingFlags flags) => 
        GetFunctions(flags).FirstOrDefault(m => m.Name == name);
    
}

public class CppMemberFunctionInfo(string name, ICppFunction[] functions) : ICppMemberInfo
{
    public string Name => name;

    private CppCallableValue _dummyValue = new CppCallableValue(functions);
    
    public ICppType MemberType => _dummyValue.GetCppType;

    public ICppValue GetValue(ICppValue instance) => new CppCallableValue(instance, functions);

    public ICppValue Invoke(ICppValue? instance, params ICppValue[] args)
    {
        var overload = GetOverload(instance?.GetCppType, args.Select(x => x.GetCppType));
        if (overload is null)
            throw new Exception($"Function '{Name}' has no matching overload'");

        return overload.Invoke(instance, args);
    }

    public ICppFunction? GetOverload(ICppType? instance, IEnumerable<ICppType> args) =>
        functions.FirstOrDefault(x =>
            SameInstanceType(x.InstanceType, instance)
            && x.ParameterTypes
                .ZipFill(args.Select(y => y))
                .All(y => y.Left is not null && y.Left.Type.Equals(y.Right)));

    private bool SameInstanceType(ICppType? a, ICppType? b)
    {
        if (a is null)
            return b is null;
        return a.Equals(b);
    }
}

public static class CppTypeExtensions
{
    extension(ICppType type)
    {

        public bool TryGetFunctionOverload(
            string name,
            CppMemberBindingFlags flags,
            ICppType[] args, 
            [NotNullWhen(true)] out ICppFunction? function)
        {
            if (type.GetFunction(name, flags) is not { } functionInfo)
            {
                function = null;
                return false;
            }

            var instanceType = ((flags & CppMemberBindingFlags.Instance) != 0)
                ? type
                : null;

            if (functionInfo.GetOverload(instanceType, args) is not { } overload)
            {
                function = null;
                return false;
            }

            function = overload;
            return true;
        }
        
    }
}


public abstract class CppPrimitiveType : ICppType
{
    private ICppMemberInfo[] _members;
    
    protected CppPrimitiveType(string name, ICppFunction[]? functions = null)
    {
        Name = name;
        if (functions is not null)
            Functions = functions;
        
        _members = functions?
            .GroupBy(x => x.Name)
            .Select(ICppMemberInfo (x) => new CppMemberFunctionInfo(x.Key, x.ToArray()))
            .ToArray() ?? [];
        // typeof(int).GetMethod("").

        // MethodInfo mi = null!;
        // mi.In
    }
    
    public string Name { get; }

    public ICppConstructor[] Constructor { get; init; } = [];
    public ICppFunction[] Functions { get; } = [];
    public ICppConverter[] Converter { get; init; } = [];
    
    public bool IsAssignableTo(ICppType other) => other.GetType() == GetType();
    
    public bool Equals(ICppType? other) => Name == other?.Name;

    public abstract ICppValue Create();

    // TODO: Implement accessibility
    public IEnumerable<ICppMemberInfo> GetMembers(CppMemberBindingFlags flags) => _members;

    public IEnumerable<CppMemberFunctionInfo> GetFunctions(CppMemberBindingFlags flags)
    {
        foreach (var function in Functions.GroupBy(x => x.Name))
        {
            yield return new CppMemberFunctionInfo(function.Key, function.ToArray());
        }
    }
}

// TODO: Make all types singletons / always use CppTypes.<type> 

public sealed class CppVoidType : CppPrimitiveType
{
    public static CppVoidType Instance { get; }= new CppVoidType();
    
    private CppVoidType() : base("void")
    {
        Constructor = [ new ConstructorFunction<CppVoidValueT>(() => new CppVoidValueT() ) ];
    }

    public override ICppValue Create() => new CppVoidValueT();
};


public sealed class CppBoolType : CppPrimitiveType
{
    public static CppBoolType Instance { get; } = new CppBoolType();
    
    private static ICppFunction[] MemberFunctions() =>
    [
        ..CppCommonOperators.EquatorOperators<CppBoolValueT, bool>(),
        CppCommonOperators.PrimitiveAssignment<CppBoolValueT, bool>(),
        // new MemberFunction<CppBoolValue, CppBoolValue, CppBoolValue>("operator&&", 
        //     (a, b) => new CppBoolValue(a.Value && b.Value)),
        // new MemberFunction<CppBoolValue, CppBoolValue, CppBoolValue>("operator||", 
        //     (a, b) => new CppBoolValue(a.Value || b.Value))
        new MemberFunction<CppBoolValueT, CppBoolValueT>("operator!", a => new CppBoolValueT(!a.Value))
    ];
    
    private CppBoolType() : base("bool", MemberFunctions())
    {
        
    }

    public override ICppValue Create() => new CppBoolValueT(false);
}



