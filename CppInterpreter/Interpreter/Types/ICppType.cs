using System.Diagnostics.CodeAnalysis;
using CppInterpreter.Helper;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

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
    ICppValue GetValue(ICppValue instance);
}

public class CppMemberFunctionInfo(string name, ICppFunction[] functions) : ICppMemberInfo
{
    public string Name => name;

    private readonly CppCallableValue _dummyValue = new CppCallableValue(functions);
    
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
            && x.ParametersMatch(args));

    private bool SameInstanceType(ICppType? a, ICppType? b)
    {
        if (a is null)
            return b is null;
        return a.Equals(b);
    }
}