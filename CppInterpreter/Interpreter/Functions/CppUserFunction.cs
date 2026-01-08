using CppInterpreter.Ast;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Functions;


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
        
        if (ParameterTypes.ParametersMissMatch(parameters))
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
