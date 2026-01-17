using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public class CppUserType : ICppType
{
    private readonly AstCompoundTypeDefinition _astNode;
    private readonly List<MemberData> _members = [];
    private readonly List<MemberValue> _values = [];
    private readonly List<MemberFunction> _functions = [];
    private readonly List<ICppFunction> _constructors = [];
    private ICppFunction? _defaultConstructor;
    
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

    public Scope<ICppValue> CreateParserScope()
    {
        var scope = new Scope<ICppValue>(Closure);

        foreach (var member in _values)
        {
            scope.TryAddSymbol(member.MemberInfo.Name, member.MemberInfo.MemberType.Create());
        }

        foreach (var function in _functions)
        {
            scope.TryBindFunction(function.Name, function.Function);
        }
        
        return scope;
    }
    
    public ICppValue Create()
    {
        if (_defaultConstructor == null)
                throw new Exception("Type has no default constructor");
        return _defaultConstructor.Invoke(null, []);
    }

    public ICppValue CreateParserDummy()
    {
        var instance = new CppUserValue(this);

        foreach (var member in _members)
        {
            instance.AddMember(member.MemberInfo.Name, member.MemberInfo.MemberType.CreateParserDummy());
        }
        
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
        return _functions.GroupBy(x => x.Name)
            .Select(x => new CppMemberFunctionInfo(
                x.Key,
                x.Where(y =>
                        (y.Visibility == MemberVisibility.Public && flags.HasFlag(CppMemberBindingFlags.Public))
                        ||
                        (y.Visibility != MemberVisibility.Public && flags.HasFlag(CppMemberBindingFlags.NonPublic)))
                    .Select(y => y.Function)
                    .ToArray())
        );
    }

    
    public void BuildMembers(
        Scope<ICppValue> closure,  
        Action<ICppUserTypeMemberBuilder, AstCompoundTypeDefinition, Scope<ICppValue>> builder)
    {
        Closure = closure;
        builder(new Builder(this), _astNode, closure);
    }
    
    public void BuildMemberFunctions(Func<object> builder)
    {
        
    }

    private class Builder(CppUserType instance) : ICppUserTypeMemberBuilder
    {

        public void AddVariable(string name, ICppType value, InterpreterExpressionResult? initializer, MemberVisibility visibility)
        {
            var memberValue = new CppMemberValue(name, value);
            instance._members.Add(new MemberData(memberValue, visibility));
            instance._values.Add(new MemberValue(memberValue, initializer));
        }

        public IEnumerable<MemberValue> Variables => instance._values;
        
        public void AddFunction(string name, ICppFunction func, MemberVisibility visibility)
        {
            instance._functions.Add(new MemberFunction(name, func, visibility));
        }

        public void AddConstructor(ICppFunction constructorFunction)
        {
            if (constructorFunction.ReturnType != instance)
                throw new Exception("Constructor must return instance of itself");
            
            instance._constructors.Add(constructorFunction);

            if (constructorFunction.ParameterTypes.Length == 0)
                instance._defaultConstructor = constructorFunction;
        }

        public IEnumerable<ICppFunction> Constructors => instance._constructors;
    }
    
    private record MemberData(ICppMemberInfo MemberInfo, MemberVisibility Visibility);

    public record MemberValue(ICppMemberInfo MemberInfo, InterpreterExpressionResult? Initializer);
    
    private record MemberFunction(string Name, ICppFunction Function, MemberVisibility Visibility);
}

public enum MemberVisibility
{
    Public,
    Private,
    Protected,
}


public interface ICppUserTypeMemberBuilder
{
    void AddVariable(string name, ICppType value, InterpreterExpressionResult? initializer, MemberVisibility visibility);
    IEnumerable<CppUserType.MemberValue> Variables { get; }
    
    void AddFunction(string name, ICppFunction func, MemberVisibility visibility);

    void AddConstructor(ICppFunction constructorFunction);
    IEnumerable<ICppFunction> Constructors { get; }
}