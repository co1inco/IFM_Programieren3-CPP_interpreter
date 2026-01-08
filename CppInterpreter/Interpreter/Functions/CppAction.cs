using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Functions;

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
        
        return new CppVoidValue();
    }
}