using CppInterpreter.CppParser;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter;

public interface ICppExpression
{
    ICppValueBase Evaluate();
}


public delegate ICppValueBase CppExpression();


public class CppLiteralExpression(ICppValueBase value) : ICppExpression
{
    public ICppValueBase Evaluate() => value;
}

public class CppBinOpExpression(ICppExpression left, ICppExpression right, string operation) : ICppExpression
{
    public ICppValueBase Evaluate()
    {
        var l = left.Evaluate();
        var r = right.Evaluate();

        var function = l.Type.GetFunction(operation, r.Type);
        
        return function.Invoke(l, [r]);
    }
}

public class CppAssignmentExpression(string name, ICppExpression value, CppStage1Scope scope) : ICppExpression
{
    public ICppValueBase Evaluate()
    {
        if (!scope.Values.TryGetSymbol(name, out var variable))
            throw new Exception($"Variable not found '{name}'");

        var exprValue = value.Evaluate();
        var function = variable.Type.GetFunction("operator=", [exprValue.Type]);

        function.Invoke(variable, [exprValue]);
        return variable;
    }
}

public class CppAtomStatement(string name, CppStage1Scope scope) : ICppExpression
{
    public ICppValueBase Evaluate()
    {
        if (!scope.Values.TryGetSymbol(name, out var variable))
            throw new Exception($"Variable not found '{name}'");
        
        return variable;
    }
}