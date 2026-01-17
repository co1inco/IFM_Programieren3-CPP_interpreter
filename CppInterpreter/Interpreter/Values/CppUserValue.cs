using CppInterpreter.CppParser;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public class CppUserValue : ICppValue
{
    private readonly Dictionary<string, ICppValue> _memberValues = [];
    
    public CppUserValue(ICppType type)
    {
        GetCppType = type;
    }
    
    // public Dictionary<string, ICppValue> MemberValues { get; } = [];
    public IReadOnlyDictionary<string, ICppValue> MemberValues => _memberValues;
    
    public ICppType GetCppType { get; }
    public string StringRep() => "<object>";
    public bool ToBool() => true;

    //TODO: initialize scope with this pointer
    public Scope<ICppValue> InstanceScope { get; } = new();

    public void AddMember(string name, ICppValue value)
    {
        _memberValues[name] = value;
        InstanceScope.TryBindSymbol(name, value);
    }
    
    
    // TODO: this should? check for a copy  
    public ICppValue Copy()
    {
        // TODO: look for the copy constructor instead
        var instance = new CppUserValue(GetCppType);
        foreach (var memberValue in MemberValues)
        {
            instance.AddMember(memberValue.Key, memberValue.Value);
        }
        return instance;
    }
}




public sealed class DefaultCopyConstructor(CppUserType type) : ICppFunction
{
    public string Name => type.Name;
    public ICppType ReturnType => type;
    public ICppType? InstanceType => null;

    public CppFunctionParameter[] ParameterTypes { get; } =
    [
        new CppFunctionParameter("other", type, true)
    ];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not null)
            throw new Exception("Function is not a member function");

        if (parameters is not [CppUserValue other ])
            throw new Exception("Invalid arguments");
        
        // TODO: Validate that other is assignable to this (ie. has all values that are required for copy)

        var newInstance = new CppUserValue(type);
        foreach (var member in type.GetFields(CppMemberBindingFlags.AnyInstance))
        {
            newInstance.AddMember(member.Name, other.MemberValues[member.Name].Copy());
        }
        
        return newInstance;
    }
}

public sealed class DefaultAssignmentOperator(CppUserType type) : ICppFunction
{
    public string Name => "operator=";
    public ICppType ReturnType => type;
    public ICppType? InstanceType => type;
    public CppFunctionParameter[] ParameterTypes { get; } = 
    [
        new CppFunctionParameter("other", type, true)
    ];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {   
        if (instance is not CppUserValue userInstance)
            throw new Exception("Instance is not a user type");
        
        if (parameters is not [CppUserValue other ])
            throw new Exception("Invalid arguments");

        foreach (var member in userInstance.GetCppType.GetFields(CppMemberBindingFlags.AnyInstance))    
        {
            userInstance.AddMember(member.Name, other.MemberValues[member.Name].Copy());
        }
        
        return instance;
    }
}

public sealed class BaseUserTypeConstructor : ICppFunction
{
    private readonly Dictionary<string, InterpreterExpressionResult> _initializers;
    private readonly ICppFunction? _userFunction;
    private readonly Scope<ICppValue> _closure;
    
    public BaseUserTypeConstructor(
        CppUserType instanceType,
        Scope<ICppValue> closure,
        IEnumerable<(string Name, InterpreterExpressionResult InitialValue)> initializers,
        ICppFunction? userFunction)
    {
        if (!userFunction?.ReturnType.Equals(CppTypes.Void) ?? false)
            throw new ArgumentException("User constructor functions must return void");
        
        Name = instanceType.Name;
        ReturnType = instanceType;
        _userFunction = userFunction;
        ParameterTypes = userFunction?.ParameterTypes ?? [];

        _closure = closure;
        _initializers = initializers.ToDictionary(x => x.Name, x => x.InitialValue);
    }

    
    public string Name { get; }
    public ICppType ReturnType { get; }
    public ICppType? InstanceType => null;
    public CppFunctionParameter[] ParameterTypes { get; }
    
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not null)
            throw new Exception("Base constructor is not a member function");

        var newInstance = new CppUserValue(ReturnType);

        //TODO: base class constructor before
        
        foreach (var member in ReturnType.GetFields(CppMemberBindingFlags.AnyInstance))
        {
            if (_initializers.TryGetValue(member.Name, out var initializer))
                newInstance.AddMember(member.Name, initializer.Eval(_closure));
            else
                newInstance.AddMember(member.Name, member.MemberType.Create());
        }

        _userFunction?.Invoke(newInstance, parameters);
        
        return newInstance;
    }
}