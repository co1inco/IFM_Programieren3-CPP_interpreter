using System.Diagnostics.CodeAnalysis;
using CppInterpreter.Helper;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public interface ICppType : IEquatable<ICppType>
{
    string Name { get; }   

    ICppConstructor[] Constructor { get; }
    ICppConverter[] Converter { get; }

    public bool IsAssignableTo(ICppType other);

    ICppValue Create();

    ICppValue CreateParserDummy();

    
    /// <summary>
    /// Generate a scope with dummy values that can be used for validation during parsing
    /// </summary>
    /// <returns></returns>
    Scope<ICppValue> CreateParserScope() => new();

    IEnumerable<ICppMemberInfo> GetMembers(CppMemberBindingFlags flags);

    ICppMemberInfo? GetMember(string name, CppMemberBindingFlags flags) => 
        GetMembers(flags)
            .FirstOrDefault(m => m.Name == name);

    
    IEnumerable<CppMemberFunctionInfo> GetFunctions(CppMemberBindingFlags flags);
    CppMemberFunctionInfo? GetFunction(string name, CppMemberBindingFlags flags) => 
        GetFunctions(flags).FirstOrDefault(m => m.Name == name);
    
    IEnumerable<CppMemberValue> GetFields(CppMemberBindingFlags flags);
    CppMemberValue? GetField(string name, CppMemberBindingFlags flags) => 
        GetFields(flags).FirstOrDefault(m => m.Name == name);
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
        
        public ICppValueT Construct<T>(params ICppValueT[] parameters) where T : ICppValueT
        {
            var parameterTypes = parameters.Select<ICppValue, ICppType>(x => x.GetCppType).ToArray();
            
            var ctor = T.TypeOf.Constructor.FirstOrDefault(x =>
                x.ParameterTypes.FunctionParametersMatch(parameterTypes));
            
            if (ctor is null)
                throw new Exception($"Constructor '{typeof(T)}' not found");
            
            return ctor.Construct(parameters);
        }
        
        public ICppValueT Construct(params ICppValue[] parameters)
        {
            var parameterTypes = parameters.Select<ICppValue, ICppType>(x => x.GetCppType).ToArray();
            
            var ctor = type.Constructor.FirstOrDefault(x =>
                x.ParameterTypes.FunctionParametersMatch(parameterTypes));
            
            if (ctor is null)
                throw new Exception($"Constructor '{type}' not found");
            
            return ctor.Construct(parameters);
        }
    }
}


public interface ICppMemberInfo
{
    string Name { get; }
    ICppType MemberType { get; }
    MemberVisibility Visibility { get; }
    
    ICppValue GetValue(ICppValue instance);
}

public class CppMemberFunctionInfo(string name, MemberVisibility visibility, ICppFunction[] functions) : ICppMemberInfo
{
    public string Name => name;

    private readonly CppCallableValue _dummyValue = new CppCallableValue(functions);
    
    public ICppType MemberType => _dummyValue.GetCppType;
    public MemberVisibility Visibility => visibility;

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
            && x.ParametersMatch(args));

    private bool SameInstanceType(ICppType? a, ICppType? b)
    {
        if (a is null)
            return b is null;
        return a.Equals(b);
    }
}


public class CppMemberValue(string name, MemberVisibility visibility, ICppType type, ICppType instanceType) : ICppMemberInfo
{
    public string Name { get; } = name;
    public ICppType MemberType { get; } = type;
    public ICppType InstanceType { get; } = instanceType;
    public MemberVisibility Visibility => visibility;

    public ICppValue GetValue(ICppValue instance)
    {
        var userValue = FindUserValue(instance);       
        
        if (!userValue.MemberValues.TryGetValue(Name, out var value))
            throw new Exception($"Member value '{Name}' not found");

        return value;
    }

    private CppUserValue FindUserValue(ICppValue instance)
    {
        if (instance is not CppUserValue userValue)
            throw new Exception("Instance is not a user type");
            
        if (instance.GetCppType.Equals(InstanceType))
            return userValue;

        if (userValue.BaseValues.FirstOrDefault(x => x.GetCppType == InstanceType) is CppUserValue baseValue)
            return baseValue;
        
        throw new Exception("Instance is not a user type");
    }
}
