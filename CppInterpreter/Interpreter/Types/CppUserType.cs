using CppInterpreter.Ast;
using CppInterpreter.CppParser;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Values;
using CSharpFunctionalExtensions;

namespace CppInterpreter.Interpreter.Types;

public class CppUserType : ICppType
{
    // private readonly List<MemberData> _members = [];
    private readonly List<MemberValue> _values = [];
    private readonly List<MemberFunction> _functions = [];
    private readonly List<MemberConstructor> _constructors = [];
    // private ICppFunction? _defaultConstructor;
    private ICppFunction? _destructor;
    private readonly List<BaseType> _baseTypes = [];
    
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
    
    /// <summary>
    /// Create a basic type instance without any special initialization 
    /// </summary>
    /// <returns></returns>
    public ICppValue Create()
    {
        return new CppUserValue(
            this,
            _values.Select(x => (x.Name, x.Initializer())),
            _baseTypes.Select(x => x.Type.Create())
        );
    }

    public ICppValue Create(
        (string Name, ICppValue Value)[] initialValues,
        ICppValue[] baseValues
        )
    {
        return new CppUserValue(
            this,
            _values.Select(x => (
                x.Name,
                initialValues.TryFirst(y => x.Name == y.Name)
                    .Match(
                        v => v.Value,
                        () => x.Initializer())
            )),
            baseValues
        );
    }
    
    public ICppValue CreateParserDummy()
    {
        return new CppUserValue(
            this,
            _values.Select(x => (x.Name, x.MemberInfo.MemberType.CreateParserDummy())),
            []
        );
    }

    public void Destruct(CppUserValue instance)
    {
        _destructor?.Invoke(instance, []);
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
                    x.MemberInfo.MemberType,
                    this)))
            .Concat(_functions
                .Where(x => VisibilityMatches(flags, x.Visibility))
                .GroupBy(x => x.Name)
                .Select(x => new CppMemberFunctionInfo(
                    x.Key,
                    x.First().Visibility,
                    x.Select(y => y.Function).ToArray())))
            .Concat(_baseTypes
                .Where(x => VisibilityMatches(flags, x.Visibility))
                .SelectMany(x => x.Type.GetMembers(flags)));

    public IEnumerable<CppMemberFunctionInfo> GetFunctions(CppMemberBindingFlags flags) =>
        _functions
            .Where(x => VisibilityMatches(flags, x.Visibility))
            .GroupBy(x => x.Name)
            .Select(x => new CppMemberFunctionInfo(
                x.Key,
                x.First().Visibility,
                x.Select(y => y.Function).ToArray()))
        .Concat(_baseTypes
            .Where(x => VisibilityMatches(flags, x.Visibility))
            .SelectMany(x => x.Type.GetFunctions(flags)));

    public IEnumerable<CppMemberValue> GetFields(CppMemberBindingFlags flags) =>
        _values
            .Where(x => VisibilityMatches(flags, x.Visibility))
            .Select(x => new CppMemberValue(x.Name, x.Visibility, x.MemberInfo.MemberType, this))
        .Concat(_baseTypes
            .Where(x => VisibilityMatches(flags, x.Visibility))
            .SelectMany(x => x.Type.GetFields(flags)));

    // public IEnumerable<ICppFunction> Constructors(CppMemberBindingFlags flags)
    // {
    //     
    // }
    //

    public void BuildMembers(
        Scope<ICppValue> closure,  
        Action<ICppUserTypeMemberBuilder, Scope<ICppValue>> builderFunction)
    {
        Closure = closure;
        var builder = new Builder(this);
        builderFunction(builder, closure);
    }
    
    // TODO: remove
    public void BuildMemberFunctions(Func<object> builder)
    {
        
    }

    //TODO: Instead of building an existing type, the builder could create the type once.
    // To make sure that references already work, a CppDeferredType could be added where the type can later be added  
    private class Builder(CppUserType instance) : ICppUserTypeMemberBuilder
    {

        public void AddVariable(string name, ICppType type, InterpreterExpressionResult? initializer, MemberVisibility visibility)
        {
            var memberValue = new CppMemberValue(name, visibility, type, instance);
            var initializerFunc = () => initializer?.Eval(instance.Closure) ?? type.Create();
            instance._values.Add(new MemberValue(name, memberValue, visibility, initializerFunc));
        }

        public IEnumerable<MemberValue> Variables => instance._values;
        
        public void AddFunction(string name, ICppFunction func, MemberVisibility visibility)
        {
            instance._functions.Add(new MemberFunction(name, func, visibility));
        }

        public void AddConstructor(ICppFunction constructorFunction, MemberVisibility visibility)
        {
            if (constructorFunction.ReturnType != instance)
                throw new Exception("Constructor must return instance of itself");
            
            instance._constructors.Add(new MemberConstructor(constructorFunction, visibility));
        }
        
        public void SetDestructor(ICppFunction destructorFunction)
        {
            instance._destructor = destructorFunction;
        }

        public void AddBaseType(ICppType type, MemberVisibility visibility)
        {
            instance._baseTypes.Add(new BaseType(type, visibility));
        }
    }
    
    // private record MemberData(ICppMemberInfo MemberInfo, MemberVisibility Visibility);

    public record MemberValue(string Name, ICppMemberInfo MemberInfo, MemberVisibility Visibility, Func<ICppValue> Initializer);
    
    private record MemberFunction(string Name, ICppFunction Function, MemberVisibility Visibility);
    
    private record BaseType(ICppType Type, MemberVisibility Visibility);
    
    private record MemberConstructor(ICppFunction UserFunction, MemberVisibility Visibility);
}

public enum MemberVisibility
{
    Public,
    Private,
    Protected,
}


public interface ICppUserTypeMemberBuilder
{
    void AddVariable(string name, ICppType type, InterpreterExpressionResult? initializer, MemberVisibility visibility);
    IEnumerable<CppUserType.MemberValue> Variables { get; }
    
    void AddFunction(string name, ICppFunction func, MemberVisibility visibility);

    void AddConstructor(ICppFunction constructorFunction, MemberVisibility visibility);
    
    void SetDestructor(ICppFunction destructorFunction);

    void AddBaseType(ICppType type, MemberVisibility visibility);
}