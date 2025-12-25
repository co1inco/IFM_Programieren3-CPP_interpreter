using CppInterpreter.Ast;
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

    ICppValueBase Invoke(ICppValueBase? instance, ICppValueBase[] parameters);
}


public sealed class MemberAction<TInstance>(string name, Action<TInstance> action) : ICppFunction 
    where TInstance : ICppValue
{
    public string Name => name;
    public ICppType ReturnType => new CppVoidType();
    public ICppType? InstanceType => TInstance.SType;
    public CppFunctionParameter[] ParameterTypes => [];
    
    public ICppValueBase Invoke(ICppValueBase? instance, ICppValueBase[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.SType, instance?.Type);
        action(tInstance);
        
        return new CppVoidValue();
    }
}

public sealed class MemberAction<TInstance, TValue1>(string name, Action<TInstance, TValue1> action) : ICppFunction 
    where TInstance : ICppValue
    where TValue1 : ICppValue
{
    public string Name => name;
    public ICppType ReturnType => new CppVoidType();
    public ICppType? InstanceType => TInstance.SType;
    public CppFunctionParameter[] ParameterTypes => [ new CppFunctionParameter("p1", TValue1.SType, true) ];
    
    public ICppValueBase Invoke(ICppValueBase? instance, ICppValueBase[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.SType, instance?.Type);
        
        if (parameters is not [ TValue1 v1 ])
            throw new InvalidParametersException("Invalid parameters");
        
        action(tInstance, v1);
        
        return new CppVoidValue();
    }
}

public sealed class MemberFunction<TInstance, TReturn>(string name, Func<TInstance, TReturn> function) : ICppFunction 
    where TInstance : ICppValue
    where TReturn : ICppValue
{
    public string Name => name;
    public ICppType ReturnType => TReturn.SType;
    public ICppType? InstanceType => TInstance.SType;
    public CppFunctionParameter[] ParameterTypes => [];
    
    public ICppValueBase Invoke(ICppValueBase? instance, ICppValueBase[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.SType, instance?.Type);
        
        return function(tInstance);
    }
}

public sealed class MemberFunction<TInstance, TValue1, TReturn>(string name, Func<TInstance, TValue1, TReturn> function) : ICppFunction 
    where TInstance : ICppValue
    where TValue1 : ICppValue
    where TReturn : ICppValue
{
    public string Name => name;
    public ICppType ReturnType => TReturn.SType;
    public ICppType? InstanceType => TInstance.SType;
    public CppFunctionParameter[] ParameterTypes => [ new CppFunctionParameter("p1", TValue1.SType, true) ];
    
    public ICppValueBase Invoke(ICppValueBase? instance, ICppValueBase[] parameters)
    {
        if (instance is not TInstance tInstance)
            throw new InvalidTypeException(TInstance.SType, instance?.Type);
        
        if (parameters is not [ TValue1 v1 ])
            throw new InvalidParametersException("Invalid parameters");
        
        return function(tInstance, v1);
    }
}

public sealed class CppAction<TValue1>(string name, Action<TValue1> action) : ICppFunction
    where TValue1 : ICppValue
{
    public string Name => name;

    public ICppType ReturnType => CppTypes.Void;
    public ICppType? InstanceType => null;
    public CppFunctionParameter[] ParameterTypes => [ new CppFunctionParameter("p1", TValue1.SType, true) ];
    public ICppValueBase Invoke(ICppValueBase? instance, ICppValueBase[] parameters)
    {
        if (instance is not null)
            throw new Exception("Function is not a member function");
        
        if (parameters is not [ TValue1 v1 ])
            throw new Exception("Invalid parameters");

        action(v1);
        
        return new CppVoidValue();
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
    
    
    public ICppValueBase Invoke(ICppValueBase? instance, ICppValueBase[] parameters)
    {
        if (instance is not null)
            throw new Exception("Function is not a member function");
        
        if (parameters.ZipFill(ParameterTypes).Any(x => !x.Left?.Type.Equals(x.Right?.Type) ?? false))
            throw new Exception("Invalid parameters");

        if (Function is null || Closure is null)
            throw new Exception("Function was not build");

        var functionScope = new Scope<ICppValueBase>(Closure);
        
        foreach (var (value, parameter) in parameters.Zip(ParameterTypes))
        {
            var v = parameter.IsReference
                ? value
                : parameter.Type.Construct(value); // copy constructor
            
            functionScope.TryBindSymbol(parameter.Name, v);
        }
        
        return Function.Invoke(functionScope);
    }
    
    public AstBlock Body { get; }
    
    public Func<Scope<ICppValueBase>, ICppValueBase>? Function { get; private set; }
    public Scope<ICppValueBase>? Closure { get; private set; }
    
    public void BuildBody(Scope<ICppValueBase> closure, Func<AstBlock, Func<Scope<ICppValueBase>, ICppValueBase>> builder)
    {
        Closure = closure;
        Function = builder(Body);
    }
}



public interface ICppConstructor
{
    ICppType InstanceType { get; }
    ICppType[] ParameterTypes { get; }
    ICppValue Construct(ICppValueBase[] parameters);
};

public sealed class ConstructorFunction<TInstance>(Func<TInstance> constructor) : ICppConstructor
    where TInstance : ICppValue
{
    public ICppType InstanceType => TInstance.SType;
    public ICppType[] ParameterTypes => [ ];

    public ICppValue Construct(ICppValueBase[] parameters) => constructor();
}

public sealed class ConstructorFunction<TInstance, TValue1>(Func<TValue1, TInstance> constructor, bool @explicit = false) : ICppConstructor
    where TInstance : ICppValue
    where TValue1 : ICppValue
{
    public ICppType InstanceType => TInstance.SType;
    public ICppType[] ParameterTypes => [ TValue1.SType ];
    public ICppValue Construct(ICppValueBase[] parameters)
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