using CppInterpreter.CppParser;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public class CppUserValue : ICppValue, IDisposable
{
    private readonly Dictionary<string, ICppValue> _memberValues = [];
    
    public CppUserValue(
        ICppType type, 
        IEnumerable<(string, ICppValue)> initialValues, 
        IEnumerable<ICppValue> baseValues)
    {
        GetCppType = type;
        //TODO: initialize scope with "this" pointer
        InstanceScope = new();

        foreach (var (name, value) in initialValues)
        {
            _memberValues[name] = value;
            InstanceScope.TryBindSymbol(name, value);
        }
        
        BaseValues = baseValues.ToArray();
    }
    
    // public Dictionary<string, ICppValue> MemberValues { get; } = [];
    public IReadOnlyDictionary<string, ICppValue> MemberValues => _memberValues;
    
    public ICppValue[] BaseValues { get; }
    
    public ICppType GetCppType { get; }
    public string StringRep() => "<object>";
    public bool ToBool() => true;

    
    public Scope<ICppValue> InstanceScope { get; }

    
    // TODO: this should? check for a copy  
    public ICppValue Copy()
    {
        // TODO: look for the copy constructor instead
         
        return new CppUserValue(
            GetCppType, 
            _memberValues.Select(x => (x.Key, x.Value.Copy())),
            []
        );
    }

    public void Assign(ICppValue value)
    {
        var function = GetCppType.GetFunction("operator=", CppMemberBindingFlags.AnyInstance);
        if (function is null)
            throw new Exception("Type has no assignment operator");
        function.Invoke(this, value);
    }

    public void Dispose()
    {
        if (GetCppType is CppUserType userType)
            userType.Destruct(this);
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
        if (instance is not CppUserValue targetInstance)
            throw new Exception("Instance is not a user type");
        
        if (parameters is not [CppUserValue other ])
            throw new Exception("Invalid arguments");

        foreach (var member in targetInstance.GetCppType.GetFields(CppMemberBindingFlags.AnyInstance))
        {
            targetInstance.MemberValues[member.Name].Assign(other.MemberValues[member.Name]);
        }
        
        return instance;
    }
}

public sealed class BaseUserTypeConstructor : ICppFunction
{
    private readonly Dictionary<string, InterpreterExpressionResult> _initializers;
    private readonly ICppFunction? _userFunction;
    private readonly Scope<ICppValue> _closure;
    private readonly CppUserType _userType;
    
    public BaseUserTypeConstructor(
        CppUserType instanceType,
        Scope<ICppValue> closure,
        IEnumerable<(string Name, InterpreterExpressionResult InitialValue)> initializers,
        ICppFunction? userFunction)
    {
        if (!userFunction?.ReturnType.Equals(CppTypes.Void) ?? false)
            throw new ArgumentException("User constructor functions must return void");
        
        Name = instanceType.Name;
        _userType = instanceType;
        _userFunction = userFunction;
        ParameterTypes = userFunction?.ParameterTypes ?? [];

        _closure = closure;
        _initializers = initializers.ToDictionary(x => x.Name, x => x.InitialValue);
    }

    
    public string Name { get; }
    public ICppType ReturnType => _userType;
    public ICppType? InstanceType => null;
    public CppFunctionParameter[] ParameterTypes { get; }
    
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not null)
            throw new Exception("Base constructor is not a member function");

        

        //TODO: base class constructor before

        var newInstance = _userType.Create(
            _initializers
                .Select(x => (x.Key, x.Value.Eval(_closure)))
                .ToArray(),
            []
        );

        _userFunction?.Invoke(newInstance, parameters);
        
        return newInstance;
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

        return other.Copy();
    }
}