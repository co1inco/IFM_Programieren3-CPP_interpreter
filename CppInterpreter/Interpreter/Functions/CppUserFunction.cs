using CppInterpreter.Ast;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Functions;


public sealed class CppUserFunction : ICppFunction
{
    public CppUserFunction(
        string name,
        ICppType returnType, 
        ICppType? instanceType,
        CppFunctionParameter[] arguments)
    {
        Name = name;
        ReturnType = returnType;
        InstanceType = instanceType;
        ParameterTypes = arguments;
    }
    
    public string Name { get; }
    public ICppType ReturnType { get; }
    public ICppType? InstanceType { get; }
    public CppFunctionParameter[] ParameterTypes { get; }
    
    
    public ICppValue Invoke(ICppValue? instance, ICppValue[] parameters)
    {
        if (InstanceType is not null && instance is null)
            throw new Exception("No instance provided but function is an instance member");
        if (InstanceType is not null && !InstanceType.Equals(instance?.GetCppType)) // TODO: should actually check if it is assignable to
            throw new Exception("Incorrect instance value provided");
        if (InstanceType is null &&  instance is not null)
            throw new Exception("Function is not a member function");
        
        if (ParameterTypes.ParametersMissMatch(parameters))
            throw new Exception("Invalid parameters");

        if (Function is null || Closure is null)
            throw new Exception("Function was not build");

        return Function.Invoke(BuildInterpreterScope(parameters, instance));
    }

    private Scope<ICppValue> BuildParserScope()
    {
        var scope = new Scope<ICppValue>(InstanceType is not null 
            ? new IntermediateScope<ICppValue>(Closure, InstanceType.CreateParserScope()) 
            : Closure
        );

        
        foreach (var parameter in ParameterTypes)
        {
            if (!scope.TryBindSymbol(parameter.Name, parameter.Type.Create()))
                throw new Exception("Duplicate parameter name");
        }
        
        return scope;
    }

    private Scope<ICppValue> BuildInterpreterScope(ICppValue[] parameters, ICppValue? instance)
    {
        var scope = new Scope<ICppValue>(instance is not null 
            ? new IntermediateScope<ICppValue>(Closure, instance.InstanceScope) 
            : Closure
        );
        
        foreach (var (value, parameter) in parameters.Zip(ParameterTypes))
        {
            var v = parameter.IsReference
                ? value
                : value.Copy(); // parameter.Type.Construct(value); // copy constructor
            
            scope.TryBindSymbol(parameter.Name, v);
        }
        
        return scope;
    }
    
    public Func<Scope<ICppValue>, ICppValue>? Function { get; private set; }
    public Scope<ICppValue>? Closure { get; private set; }
    
    public void BuildBody(
        AstBlock body,
        Scope<ICppValue> closure, 
        Scope<ICppType> typeScope, 
        Func<AstBlock, ICppType, Scope<ICppValue>, Scope<ICppType>, Func<Scope<ICppValue>, ICppValue>> builder)
    {
        Closure = closure;
        Function = builder(body, ReturnType, BuildParserScope(), typeScope);
    }
}
