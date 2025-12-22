using CppInterpreter.Ast;
using CppInterpreter.Interpreter;

namespace CppInterpreter.CppParser;


public delegate ICppValueBase? InterpreterStatement(Scope<ICppValueBase> scope);
public delegate ICppValueBase InterpreterExpression(Scope<ICppValueBase> scope);

public class Stage3Parser
{

    public static InterpreterStatement ParseProgram(Stage2SymbolTree program)
    {
        var statements = program.Statement
            .Select(x => x.Match<InterpreterStatement>(
                e =>
                {
                    var expr = ParseExpression(e);
                    return s => expr(s);
                },
                i =>
                {
                    var def = ParseVariableDefinition(i);
                    return s => def(s);
                }
            ));

        return s =>
        {
            ICppValueBase? last = null;
            foreach (var statement in statements)
            {
                last = statement(s);
            }

            return last;
        };
    }

    public static InterpreterStatement ParseVariableDefinition(Stage2VarDefinition definition)
    {
        var initializer = definition.Initializer is null
            ? null
            : ParseAssignment(new AstAssignment(
                new AstIdentifier(definition.Name), 
                definition.Initializer));
        
        return scope =>
        {
            var instance = definition.Type.Create();
            if (!scope.TryBindSymbol(definition.Name, instance))
                throw new Exception($"Variable '{definition.Name}' was already defined");

            return initializer?.Invoke(scope);
        };
    }
    
    public static InterpreterExpression ParseExpression(AstExpression expression) =>
        expression.Match(
            ParseLiteral,
            ParseAtom,
            ParseAssignment,
            ParseBinOp,
            unary => throw new NotImplementedException()
        );

    public static InterpreterExpression ParseAtom(AstAtom atom) => s =>
        {
            if (!s.TryGetSymbol(atom.Value, out var variable))
                throw new Exception($"Variable not found '{atom.Value}'");

            return variable;
        };

    public static InterpreterExpression ParseAssignment(AstAssignment assignment)
    {
        var inner = ParseExpression(assignment.Value);
        return scope =>
        {
            var name = assignment.Target.Value;
            
            if (!scope.TryGetSymbol(name, out var variable))
                throw new Exception($"Variable not found '{name}'");

            var exprValue = inner(scope);
            var function = variable.Type.GetFunction("operator=", [exprValue.Type]);

            function.Invoke(variable, [exprValue]);
            return variable;
        };
    }
        


    public static InterpreterExpression ParseBinOp(AstBinOp op)
    {
        var left = ParseExpression(op.Left);
        var right = ParseExpression(op.Right);
        var function = op.Operator.Match(
            e => e switch
            {
                AstBinOpOperator.Equatable.Equal => "==",
                AstBinOpOperator.Equatable.NotEqual => "!=",
                _ => throw new ArgumentOutOfRangeException(nameof(e), e, null)
            },
            c => c switch
            {
                AstBinOpOperator.Comparable.LessThan => "<",
                AstBinOpOperator.Comparable.LessThanOrEqual => "<=",
                AstBinOpOperator.Comparable.GreaterThan => ">",
                AstBinOpOperator.Comparable.GreaterThanOrEqual => ">=",
                _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
            },
            i => i switch
            {
                AstBinOpOperator.IntegerOp.BitAnd => "&",
                AstBinOpOperator.IntegerOp.BitOr => "|",
                AstBinOpOperator.IntegerOp.BitXor => "^",
                _ => throw new ArgumentOutOfRangeException(nameof(i), i, null)
            },
            b => b switch
            {
                AstBinOpOperator.BoolOp.And => "&&",
                AstBinOpOperator.BoolOp.Or => "||",
                _ => throw new ArgumentOutOfRangeException(nameof(b), b, null)
            },
            a => a switch
            {
                AstBinOpOperator.Arithmetic.Add => "+",
                AstBinOpOperator.Arithmetic.Subtract => "-",
                AstBinOpOperator.Arithmetic.Multiply => "*",
                AstBinOpOperator.Arithmetic.Divide => "/",
                AstBinOpOperator.Arithmetic.Modulo => "%",
                _ => throw new ArgumentOutOfRangeException(nameof(a), a, null)
            }
        );
        
        return scope =>
        {
            var l = left(scope);
            var r = right(scope) ;
            
            var f = l.Type.GetFunction($"operator{function}", r.Type);
        
            return f.Invoke(l, [r]);
        };
    }
    
    public static InterpreterExpression ParseLiteral(AstLiteral literal) => 
        literal.Match<InterpreterExpression>(
            c => throw new NotImplementedException(),
            i => _ => new CppInt32Value(i),
            s => throw new NotImplementedException(),
            b => _ => new CppBoolValue(b)
        );
}