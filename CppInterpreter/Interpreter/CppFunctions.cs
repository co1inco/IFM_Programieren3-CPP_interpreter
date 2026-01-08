using CppInterpreter.Ast;
using CppInterpreter.Helper;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter;

public record CppFunctionParameter(string Name, ICppType Type, bool IsReference);

public interface ICppFunction
{
    string Name { get; }
    ICppType ReturnType { get; }
    ICppType? InstanceType { get; } // TODO: remove instance type from ICppFunction
    CppFunctionParameter[] ParameterTypes { get; }

    ICppValue Invoke(ICppValue? instance, ICppValue[] parameters);
}


public sealed class MemberAction<TInstance>(string name, Action<TInstance> action) : ICppFunction 
    where TInstance : ICppValueT
{
    public string Name => name;
    public ICppType ReturnType => CppTypes.Void;
    public ICppType? InstanceType => TInstance.TypeOf;
    public CppFunctionParameter[] ParameterTypes => [];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.TypeOf, instance?.GetCppType);
        action(tInstance);
        
        return new CppVoidValueT();
    }
}

public sealed class MemberAction<TInstance, TValue1>(string name, Action<TInstance, TValue1> action) : ICppFunction 
    where TInstance : ICppValueT
    where TValue1 : ICppValueT
{
    public string Name => name;
    public ICppType ReturnType => CppTypes.Void;
    public ICppType? InstanceType => TInstance.TypeOf;
    public CppFunctionParameter[] ParameterTypes => [ new CppFunctionParameter("p1", TValue1.TypeOf, true) ];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.TypeOf, instance?.GetCppType);
        
        if (parameters is not [ TValue1 v1 ])
            throw new InvalidParametersException("Invalid parameters");
        
        action(tInstance, v1);
        
        return new CppVoidValueT();
    }
}

public sealed class MemberFunction<TInstance, TReturn>(string name, Func<TInstance, TReturn> function) : ICppFunction 
    where TInstance : ICppValueT
    where TReturn : ICppValueT
{
    public string Name => name;
    public ICppType ReturnType => TReturn.TypeOf;
    public ICppType? InstanceType => TInstance.TypeOf;
    public CppFunctionParameter[] ParameterTypes => [];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.TypeOf, instance?.GetCppType);
        
        return function(tInstance);
    }
}

public sealed class MemberFunction<TInstance, TValue1, TReturn>(string name, Func<TInstance, TValue1, TReturn> function) : ICppFunction 
    where TInstance : ICppValueT
    where TValue1 : ICppValueT
    where TReturn : ICppValueT
{
    public string Name => name;
    public ICppType ReturnType => TReturn.TypeOf;
    public ICppType? InstanceType => TInstance.TypeOf;
    public CppFunctionParameter[] ParameterTypes => [ new CppFunctionParameter("p1", TValue1.TypeOf, true) ];
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.TypeOf, instance?.GetCppType);
        
        if (parameters is not [ TValue1 v1 ])
            throw new InvalidParametersException("Invalid parameters");
        
        return function(tInstance, v1);
    }
}

public sealed class CppAction<TValue1>(string name, Action<TValue1> action) : ICppFunction
    where TValue1 : ICppValueT
{
    public string Name => name;

    public ICppType ReturnType => CppTypes.Void;
    public ICppType? InstanceType => null;
    public CppFunctionParameter[] ParameterTypes => [ new CppFunctionParameter("p1", TValue1.TypeOf, true) ];
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not null)
            throw new Exception("Function is not a member function");
        
        if (parameters is not [ TValue1 v1 ])
            throw new Exception("Invalid parameters");

        action(v1);
        
        return new CppVoidValueT();
    }
}

public sealed class CppUserFunction : ICppFunction
{
    public CppUserFunction(
        string name,
        ICppType returnType, 
        CppFunctionParameter[] arguments,
        AstBlock body)
    {
        Name = name;
        ReturnType = returnType;
        ParameterTypes = arguments;
        Body = body;
    }
    
    public string Name { get; }
    public ICppType ReturnType { get; }
    public ICppType? InstanceType => null;
    public CppFunctionParameter[] ParameterTypes { get; }
    
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (instance is not null)
            throw new Exception("Function is not a member function");
        
        if (parameters.ZipFill(ParameterTypes).Any(x => !x.Left?.GetCppType.Equals(x.Right?.Type) ?? false))
            throw new Exception("Invalid parameters");

        if (Function is null || Closure is null)
            throw new Exception("Function was not build");

        var functionScope = new Scope<ICppValue>(Closure);
        
        foreach (var (value, parameter) in parameters.Zip(ParameterTypes))
        {
            var v = parameter.IsReference
                ? value
                : parameter.Type.Construct(value); // copy constructor
            
            functionScope.TryBindSymbol(parameter.Name, v);
        }
        
        return Function.Invoke(functionScope);
    }

    public Scope<ICppValue> BuildParserScope(Scope<ICppValue> scope)
    {
        scope = new Scope<ICppValue>(scope);

        foreach (var parameter in ParameterTypes)
        {
            if (!scope.TryBindSymbol(parameter.Name, parameter.Type.Create()))
                throw new Exception("Duplicate parameter name");
        }
        
        return scope;
    }
    
    public AstBlock Body { get; }
    
    public Func<Scope<ICppValue>, ICppValue>? Function { get; private set; }
    public Scope<ICppValue>? Closure { get; private set; }
    
    public void BuildBody(Scope<ICppValue> closure, Func<AstBlock, Scope<ICppValue>, Func<Scope<ICppValue>, ICppValue>> builder)
    {
        Closure = closure;
        Function = builder(Body, BuildParserScope(Closure));
    }
}



public interface ICppConstructor
{
    ICppType InstanceType { get; }
    ICppType[] ParameterTypes { get; }
    ICppValueT Construct(ICppValue[] parameters);
};

public sealed class ConstructorFunction<TInstance>(Func<TInstance> constructor) : ICppConstructor
    where TInstance : ICppValueT
{
    public ICppType InstanceType => TInstance.TypeOf;
    public ICppType[] ParameterTypes => [ ];

    public ICppValueT Construct(ICppValue[] parameters) => constructor();
}

public sealed class ConstructorFunction<TInstance, TValue1>(Func<TValue1, TInstance> constructor, bool @explicit = false) : ICppConstructor
    where TInstance : ICppValueT
    where TValue1 : ICppValueT
{
    public ICppType InstanceType => TInstance.TypeOf;
    public ICppType[] ParameterTypes => [ TValue1.TypeOf ];
    public ICppValueT Construct(ICppValue[] parameters)
    {
        if (parameters is not [ TValue1 v1 ])
            throw new InvalidParametersException("Invalid parameters");
        
        return constructor(v1);
    }

    /// <summary>
    /// Indicates that a function should not be used for implicit conversions.
    /// Only relevant for single parameter constructors
    /// (currently not used)
    /// </summary>
    public bool Explicit => @explicit;
}

// public static class CppFunctionExtensions
// {
//     extension(ICppType type)
//     {
//         public ICppFunction GetFunction(string name, IEnumerable<ICppType> parameterTypes)
//         {
//             
//         }
//     }
//     
//     
// }