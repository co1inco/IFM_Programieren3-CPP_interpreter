using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public class CppUserValue : ICppValue
{
    public CppUserValue(ICppType type)
    {
        GetCppType = type;
    }
    
    public Dictionary<string, ICppValue> MemberValues { get; } = [];
    
    public ICppType GetCppType { get; }
    public string StringRep() => "<object>";
    public bool ToBool() => true;
    
    // TODO: this should? check for a copy  
    public ICppValue Copy()
    {
        // TODO: look for the copy constructor instead
        var instance = new CppUserValue(GetCppType);
        foreach (var memberValue in MemberValues)
        {
            instance.MemberValues.Add(memberValue.Key, memberValue.Value);
        }
        return instance;
    }
}


public class CppMemberValue(string name, ICppType type) : ICppMemberInfo
{
    public string Name { get; } = name;
    public ICppType MemberType { get; } = type;

    public ICppValue GetValue(ICppValue instance)
    {
        if (instance is not CppUserValue userValue)
            throw new Exception("Instance is not a user type");
        
        if (!userValue.MemberValues.TryGetValue(Name, out var value))
            throw new Exception($"Member value '{Name}' not found");

        return value;
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
        foreach (var member in type.GetMembers(CppMemberBindingFlags.AnyInstance))
        {
            newInstance.MemberValues[member.Name] = other.MemberValues[member.Name].Copy();
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

        foreach (var member in userInstance.GetCppType.GetMembers(CppMemberBindingFlags.AnyInstance))    
        {
            userInstance.MemberValues[member.Name] = other.MemberValues[member.Name].Copy();    
        }
        
        return instance;
    }
}