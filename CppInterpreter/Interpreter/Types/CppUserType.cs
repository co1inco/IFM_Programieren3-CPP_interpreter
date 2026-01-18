using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public class CppUserType : ICppType
{
    // private readonly List<MemberData> _members = [];
    private readonly List<MemberValue> _values = [];
    private readonly List<MemberFunction> _functions = [];
    private readonly List<ICppFunction> _constructors = [];
    private ICppFunction? _defaultConstructor;
    
    public CppUserType(string name)
    {
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
            scope.TryAddSymbol(member.MemberInfo.Name, member.MemberInfo.MemberType.CreateParserDummy());
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

        foreach (var member in _values)
        {
            instance.AddMember(member.MemberInfo.Name, member.MemberInfo.MemberType.CreateParserDummy());
        }
        
        return instance;
    }

    private static bool VisibilityMatches(CppMemberBindingFlags flags, MemberVisibility visibility) =>
        visibility switch
        {
            MemberVisibility.Public => flags.HasFlag(CppMemberBindingFlags.Public),
            MemberVisibility.Private => flags.HasFlag(CppMemberBindingFlags.NonPublic),
            MemberVisibility.Protected => flags.HasFlag(CppMemberBindingFlags.NonPublic),
            _ => throw new ArgumentOutOfRangeException()
        };
    
    public IEnumerable<ICppMemberInfo> GetMembers(CppMemberBindingFlags flags) =>
        Enumerable.Empty<ICppMemberInfo>()
            .Concat(_values
                .Where(x => VisibilityMatches(flags, x.Visibility))
                .Select(x => new CppMemberValue(
                    x.Name, 
                    x.Visibility, 
                    x.MemberInfo.MemberType)))
            .Concat(_functions
                .Where(x => VisibilityMatches(flags, x.Visibility))
                .GroupBy(x => x.Name)
                .Select(x => new CppMemberFunctionInfo(
                    x.Key,
                    x.First().Visibility,
                    x.Select(y => y.Function).ToArray()))
            );

    public IEnumerable<CppMemberFunctionInfo> GetFunctions(CppMemberBindingFlags flags) =>
        _functions
            .Where(x => VisibilityMatches(flags, x.Visibility))
            .GroupBy(x => x.Name)
            .Select(x => new CppMemberFunctionInfo(
                x.Key,
                x.First().Visibility,
                x.Select(y => y.Function).ToArray()));

    public IEnumerable<CppMemberValue> GetFields(CppMemberBindingFlags flags) =>
        _values
            .Where(x => VisibilityMatches(flags, x.Visibility))
            .Select(x => new CppMemberValue(x.Name, x.Visibility, x.MemberInfo.MemberType));


    public void BuildMembers(
        Scope<ICppValue> closure,  
        Action<ICppUserTypeMemberBuilder, Scope<ICppValue>> builderFunction)
    {
        Closure = closure;
        var builder = new Builder(this);
        builderFunction(builder, closure);
    }
    
    public void BuildMemberFunctions(Func<object> builder)
    {
        
    }

    private class Builder(CppUserType instance) : ICppUserTypeMemberBuilder
    {

        public void AddVariable(string name, ICppType value, InterpreterExpressionResult? initializer, MemberVisibility visibility)
        {
            var memberValue = new CppMemberValue(name, visibility, value);
            instance._values.Add(new MemberValue(name, memberValue, visibility, initializer));
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
    
    // private record MemberData(ICppMemberInfo MemberInfo, MemberVisibility Visibility);

    public record MemberValue(string Name, ICppMemberInfo MemberInfo, MemberVisibility Visibility, InterpreterExpressionResult? Initializer);
    
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