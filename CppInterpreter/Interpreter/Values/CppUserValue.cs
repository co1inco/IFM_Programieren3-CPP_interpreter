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