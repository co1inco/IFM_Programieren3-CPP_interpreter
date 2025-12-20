namespace CppInterpreter.Interpreter;

public interface ICppExpression
{
    ICppValue Evaluate();
}


public delegate ICppValue CppExpression();


public class CppLiteralExpression(ICppValue value) : ICppExpression
{
    public ICppValue Evaluate() => value;
}

public class CppBinOpExpression(ICppExpression left, ICppExpression right, string operation) : ICppExpression
{
    public ICppValue Evaluate()
    {
        var l = left.Evaluate();
        var r = right.Evaluate();

        var function = l.Type.GetFunction(operation, l.Type, r.Type);
        
        return function.Invoke(l, [r]);
    }
}