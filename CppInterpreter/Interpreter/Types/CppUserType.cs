using CppInterpreter.Ast;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public class CppUserType : ICppType
{
    private readonly AstCompoundTypeDefinition _astNode;
    private readonly List<MemberData> _members = [];
    private readonly List<MemberValue> _values = [];
    
    public CppUserType(string name, AstCompoundTypeDefinition astNode)
    {
        _astNode = astNode;
        Name = name;
    }

    public bool Equals(ICppType? other) => other?.Name == Name;

    public string Name { get; }
    public ICppConstructor[] Constructor { get; } = [];
    public ICppConverter[] Converter { get; } = [];
    
    public Scope<ICppValue> Closure { get; private set; }
    
    public bool IsAssignableTo(ICppType other)
    {
        // return other == this;
        return other.Name == Name;
    }

    public ICppValue Create()
    {
        var instance = new CppUserValue(this);

        foreach (var value in _values)   
        {
            //TODO: initialize value
            instance.MemberValues.Add(value.MemberInfo.Name, value.MemberInfo.MemberType.Create());
        }

        // TODO: Initialize vector table?
        
        return instance;
    }

    public IEnumerable<ICppMemberInfo> GetMembers(CppMemberBindingFlags flags)
    {
        foreach (var member in _members)
        {
            switch (member.Visibility)
            {
                case MemberVisibility.Public 
                    when flags.HasFlag(CppMemberBindingFlags.Public):
                    yield return member.MemberInfo;
                    break;
                case MemberVisibility.Private or MemberVisibility.Protected
                    when flags.HasFlag(CppMemberBindingFlags.NonPublic):
                    yield return member.MemberInfo;
                    break;
            }
        }
    }

    public IEnumerable<CppMemberFunctionInfo> GetFunctions(CppMemberBindingFlags flags)
    {
        throw new NotImplementedException();
    }

    
    public void BuildMembers(
        Scope<ICppValue> closure, 
        Scope<ICppType> typeScope, 
        Action<ICppUserTypeMemberBuilder, AstCompoundTypeDefinition, Scope<ICppValue>, Scope<ICppType>> builder)
    {
        Closure = closure;
        builder(new Builder(this), _astNode, closure, typeScope);
    }
    
    public void BuildMemberFunctions(Func<object> builder)
    {
        
    }

    private class Builder(CppUserType instance) : ICppUserTypeMemberBuilder
    {

        public void AddVariable(string name, ICppType value, MemberVisibility visibility)
        {
            var memberValue = new CppMemberValue(name, value);
            instance._members.Add(new MemberData(memberValue, visibility));
            instance._values.Add(new MemberValue(memberValue));
        }

        public void AddFunction(string name, CppUserFunction func, MemberVisibility visibility)
        {
            throw new NotImplementedException();
        }
    }
    
    private record MemberData(ICppMemberInfo MemberInfo, MemberVisibility Visibility);

    private record MemberValue(ICppMemberInfo MemberInfo);
}

public enum MemberVisibility
{
    Public,
    Private,
    Protected,
}

public interface ICppUserTypeMemberBuilder
{
    void AddVariable(string name, ICppType value, MemberVisibility visibility);
    void AddFunction(string name, CppUserFunction func, MemberVisibility visibility);
}