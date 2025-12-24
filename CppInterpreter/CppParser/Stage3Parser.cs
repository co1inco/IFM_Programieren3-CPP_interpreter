using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

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
                ParseVariableDefinition,
                x => BuildFunction(x, program.TypeScope)
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

    public static InterpreterStatement BuildFunction(Stage2FuncDefinition definition, Scope<ICppType> typeScope)
    {
        definition.Function.BuildBody(definition.Closure, body =>
        {
            var statements = body.Select(x => ParseStatement(x, typeScope)).ToArray();

            return s =>
            {
                foreach (var statement in statements)
                {
                    statement(s);
                }
                // TODO: Implement returns
                return new CppVoidValue();
            };
        });
        
        return _ => new CppVoidValue();
    }

    public static InterpreterStatement ParseStatement(AstStatement statement, Scope<ICppType> typeScope)
    {
        return statement.Match<InterpreterStatement>(
            e =>
            {
                var expr = ParseExpression(e);
                return s => expr(s);
            },
            d => ParseVariableDefinition(d, typeScope),
            f => throw new Exception("Functions can not be placed here")
        );
    }

    public static InterpreterStatement ParseVariableDefinition(AstVarDefinition definition, Scope<ICppType> typeScope)
    {
        var initializer = definition.Initializer is null
            ? null
            : ParseAssignment(new AstAssignment(
                new AstIdentifier(definition.Ident.Value), 
                definition.Initializer));
        
        //TODO: Implement reference types
        if (!typeScope.TryGetSymbol(definition.AstType.Ident, out var type))
            throw new Exception($"Unknown type '{definition.AstType.Ident}'");
        
        return scope =>
        {
            var instance = type.Create();
            if (!scope.TryBindSymbol(definition.Ident.Value, instance))
                throw new Exception($"Variable '{definition.Ident.Value}' was already defined");

            return initializer?.Invoke(scope);
        };
    }
    
    public static InterpreterStatement ParseVariableDefinition(Stage2VarDefinition definition)
    {
        var initializer = definition.Initializer is null
            ? null
            : ParseAssignment(new AstAssignment(
                new AstIdentifier(definition.Name), 
                definition.Initializer));
        
        // TODO: Stage2VarDefinition creation should happen in stage 2
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
            unary => throw new NotImplementedException(),
            func => ParseFunction(func, null)
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
            s => _ => new CppStringValue(s),
            b => _ => new CppBoolValue(b)
        );

    public static InterpreterExpression ParseFunction(AstFunctionCall functionCall, Scope<ICppValueBase> scope)
    {
        // TODO: check functionCall type here
        var callable = ParseExpression(functionCall.Function);
        var arguments = functionCall.Arguments.Select(ParseExpression).ToArray();
        
        //TODO: already have the type of the callable here and validate parameters / get overload
        
        return s =>
        {
            var c = callable(s);
            var a = arguments.Select(x => x(s)).ToArray();

            if (c is not CppCallableValue callableValue)
                throw new Exception("Value was not a Callable");

            return callableValue.Invoke(a);
        };
    }
}