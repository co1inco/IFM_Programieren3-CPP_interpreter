using System.Collections.Immutable;
using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using CSharpFunctionalExtensions;

namespace CppInterpreter.CppParser;


public record StatementResult(
    InterpreterStatement Eval,
    ImmutableArray<(ICppType Type, AstMetadata Return)> ReturnValues
)
{
    public static implicit operator StatementResult(InterpreterStatement statement) =>
        new StatementResult(statement, []);
};

public record ExpressionResult(
    InterpreterExpression Eval,
    ICppType Result,
    ICppFunction[]? Functions = null)
{
    public StatementResult ToStatement() => new StatementResult(s =>
    {
        _ = Eval(s);
        return Maybe<ICppValueBase>.None;
    }, []);
}

public delegate Maybe<ICppValueBase> InterpreterStatement(Scope<ICppValueBase> scope);
public delegate ICppValueBase InterpreterExpression(Scope<ICppValueBase> scope);

public class Stage3Parser
{

    public static StatementResult ParseProgram(Stage2SymbolTree program, Scope<ICppValueBase> scope)
    {
        var statements = program.Statement
            .Select(x => x.Match(
                e => ParseExpression(e, scope).ToStatement(),
                v => ParseVariableDefinition(v, scope),
                f => BuildFunction(f, scope, program.TypeScope)
            ))
            .ToArray();

        return new StatementResult(s =>
        {
            foreach (var statement in statements)
            {
                statement.Eval(s);
            }

            return Maybe<ICppValueBase>.None;
        }, []);
    }

    
    public static StatementResult BuildFunction(Stage2FuncDefinition definition, Scope<ICppValueBase> sc, Scope<ICppType> typeScope)
    {
        definition.Function.BuildBody(definition.Closure, (body, scope) =>
        {
            var bodyStatement = ParseBlock(body, scope, typeScope, suppressBlockScope: true);

            foreach (var returnValue in bodyStatement.ReturnValues)
            {
                // TODO: for inheritance this must check if the type is assignable
                if (definition.ReturnType != returnValue.Type)
                    throw new ParserException($"Return type is '{definition.ReturnType}'", returnValue.Return);
            }
            
            // TODO: somehow detect if all paths have a return
            if (!definition.ReturnType.Equals(CppTypes.Void)  && bodyStatement.ReturnValues.Length == 0)
                throw new ParserException("Return statement is missing", body.Metadata);

            return s =>
            {
                if (bodyStatement.Eval(s).TryGetValue(out var returnValue))
                {
                    return returnValue;
                }
                
                // missing return is currently undetected if the function has at least one return
                if (!definition.ReturnType.Equals(CppTypes.Void))
                    throw new ParserException("Return statement missing", body.Metadata);

                return new CppVoidValue();
            };
        });

        return new StatementResult(_ => Maybe<ICppValueBase>.None, []);
    }

    public static StatementResult ParseStatement(AstStatement statement, Scope<ICppValueBase> scope, Scope<ICppType> typeScope)
    {
        return statement.Match<StatementResult>(
            e => ParseExpression(e, scope).ToStatement(),
            d => ParseVariableDefinition(d, scope, typeScope),
            f => throw f.CreateException("Functions must be placed at top level"),
            b => ParseBlock(b, scope, typeScope),
            r => ParseReturn(r, scope)
        );
    }

    public static StatementResult ParseBlock(AstBlock block, Scope<ICppValueBase> scope, Scope<ICppType> typeScope, bool suppressBlockScope = false)
    {
        var parseScope = suppressBlockScope ? scope : new Scope<ICppValueBase>(scope);
            
        var stmt = block.Statements
            .Select(x => ParseStatement(x, parseScope, typeScope))
            .ToArray();

        var returns = stmt.SelectMany(x => x.ReturnValues).ToArray();

        return new StatementResult(s =>
            {
                var blockScope = suppressBlockScope ? s : new Scope<ICppValueBase>(s);

                foreach (var statement in stmt)
                {
                    if (statement.Eval(blockScope).TryGetValue(out var r))
                        return Maybe.From(r);
                }

                return Maybe<ICppValueBase>.None;
            }, 
            [..returns]);

    }

    public static StatementResult ParseReturn(AstReturn returnStmt, Scope<ICppValueBase> scope)
    {
        var expression = returnStmt.ReturnValue is null
            ? new ExpressionResult(_ => new CppVoidValue(), new CppVoidType())
            : ParseExpression(returnStmt.ReturnValue, scope);

        return new StatementResult(
            s => Maybe<ICppValueBase>.From(expression.Eval(s)),
            [ (expression.Result, returnStmt.Metadata) ]
        );
    }
    
    public static StatementResult ParseVariableDefinition(AstVarDefinition definition, Scope<ICppValueBase> scope, Scope<ICppType> typeScope)
    {
        if (!typeScope.TryGetSymbol(definition.Type.Ident, out var type))
            definition.Type.ThrowNotFound();
        
        if (!scope.TryBindSymbol(definition.Ident.Value,type.Create()))
            throw new Exception($"Variable '{definition.Ident.Value}' was already defined");
        
        if (definition.Type.IsReference)
        {
            if (definition.Initializer is null)
                definition.Throw($"Declaration of reference variable '{definition.Ident.Value}' required an initializer");

            //TODO: refValue should not bind to temporary value (eg. return of function call
            var refValue = ParseExpression(definition.Initializer, scope);
            
            return new StatementResult(s =>
                {
                    var value = refValue.Eval(s);
                
                    if (!s.TryBindSymbol(definition.Ident.Value, value))
                        definition.Ident.Throw($"Variable '{definition.Ident.Value}' was already defined");

                    return Maybe<ICppValueBase>.None;
                }, []);
        }
     
        var initializer = definition.Initializer is null
            ? null
            : ParseAssignment(new AstAssignment(
                definition.Ident, 
                definition.Initializer, 
                AstMetadata.Generated()),
                scope);
        
        return new StatementResult(s =>
            {
                var instance = type.Create();
                if (!s.TryBindSymbol(definition.Ident.Value, instance))
                    definition.Ident.Throw($"Variable '{definition.Ident.Value}' was already defined");

                initializer?.Eval(s);
                
                return Maybe<ICppValueBase>.None;
            }, []);
    }
    
    public static StatementResult ParseVariableDefinition(Stage2VarDefinition definition, Scope<ICppValueBase> scope)
    {
        var initializer = definition.Initializer is null
            ? null
            : ParseAssignment(new AstAssignment(
                new AstIdentifier(definition.Name, AstMetadata.Generated()), 
                definition.Initializer, 
                AstMetadata.Generated())
                , scope);
        
        if (!scope.TryBindSymbol(definition.Name, definition.Type.Create()))
            throw new Exception($"Variable '{definition.Name}' was already defined");
        
        // TODO: Stage2VarDefinition creation should happen in stage 2
        return new StatementResult(s =>
        {
            var instance = definition.Type.Create();
            if (!s.TryBindSymbol(definition.Name, instance))
                throw new Exception($"Variable '{definition.Name}' was already defined");

            _ = initializer?.Eval(s);
            
            return Maybe<ICppValueBase>.None;
        }, []);
    }
    
    public static ExpressionResult ParseExpression(AstExpression expression, Scope<ICppValueBase> scope) =>
        expression.Match<ExpressionResult>(
            ParseLiteral,
            a => ParseAtom(a, scope),
            a => ParseAssignment(a, scope),
            b => ParseBinOp(b, scope),
            unary => throw new NotImplementedException(),
            func => ParseFunctionCall(func, scope)
        );

    public static ExpressionResult ParseAtom(AstAtom atom, Scope<ICppValueBase> scope)
    {
        if (!scope.TryGetSymbol(atom.Value, out var symbol))
            atom.Throw("Undefined value");

        var functions = symbol is CppCallableValue callable
            ? callable.Overloads.ToArray()
            : null;
        
        return new ExpressionResult(
            s =>
            {
                // Should not happen
                if (!s.TryGetSymbol(atom.Value, out var variable))
                    atom.Throw($"Variable not found '{atom.Value}'");

                return variable;
            }, 
            symbol.Type, 
            functions);
    }

    public static ExpressionResult ParseAssignment(AstAssignment assignment, Scope<ICppValueBase> scope)
    {
        var inner = ParseExpression(assignment.Value, scope);
        
        if (!scope.TryGetSymbol(assignment.Target.Value, out var target))
            assignment.Throw("Variable is not defined");
        
        if (target.Type.Equals(CppTypes.Callable))
            assignment.Throw("Can not assign to callable");

        if (!target.Type.Equals(inner.Result))
            assignment.Throw("Incompatible types");
        
        return new ExpressionResult(
            s =>
            {
                var name = assignment.Target.Value;

                if (!s.TryGetSymbol(name, out var variable))
                    assignment.Throw($"Variable is not defined");

                var exprValue = inner.Eval(s);
                var function = variable.Type.GetFunction("operator=", [exprValue.Type]);

                function.Invoke(variable, [exprValue]);
                return variable;
            },
            target.Type
        );

    }
        


    public static ExpressionResult ParseBinOp(AstBinOp op, Scope<ICppValueBase> scope)
    {
        var left = ParseExpression(op.Left, scope);
        var right = ParseExpression(op.Right, scope);
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
        
        // TODO: check if overloads exist

        return new ExpressionResult(
            s =>
            {
                var l = left.Eval(s);
                var r = right.Eval(s);

                var f = l.Type.GetFunction($"operator{function}", r.Type);

                return f.Invoke(l, [r]);
            },
            left.Result
        );


    }
    
    public static ExpressionResult ParseLiteral(AstLiteral literal) => 
        literal.Match(
            c => throw new NotImplementedException(),
            i => new ExpressionResult(_ => new CppInt32Value(i), CppTypes.Int32),
            s => new ExpressionResult(_ => new CppStringValue(s),  CppTypes.String) ,
            b => new ExpressionResult(_ => new CppBoolValue(b),  CppTypes.Boolean) 
        );

    public static ExpressionResult ParseFunctionCall(AstFunctionCall functionCall, Scope<ICppValueBase> scope)
    {
        // TODO: check functionCall type here
        var callable = ParseExpression(functionCall.Function, scope);
        var arguments = functionCall.Arguments
            .Select(x => ParseExpression(x, scope))
            .ToArray();
        
        if (!callable.Result.Equals(CppTypes.Callable))
            functionCall.Throw("Symbol is not a function");
        
        if (callable.Functions is not {} functions)
            throw functionCall.CreateException("Symbol is not a function");

        var function = functions.FirstOrDefault(x => x.ParameterTypes
            .ZipFill(arguments)
            .All(y => y.Left?.Type.Equals(y.Right?.Result) ?? false));
        
        if (function is null)
            functionCall.Throw($"No matching overload: [{string.Join(", ", arguments.Select(x => x.Result.Name))}]");
        
        //TODO: already have the type of the callable here and validate parameters / get overload
        // ParseExpression should return a dummy value or a type (callables should already know their functions)

        return new ExpressionResult(
            s =>
            {
                var c = callable.Eval(s);
                var a = arguments
                    .Select(x => x.Eval(s))
                    .ToArray();

                if (c is not CppCallableValue callableValue)
                    throw new InterpreterException($"Expected callable symbol, got '{c.Type}'", functionCall.Metadata);

                try
                {
                    return callableValue.Invoke(a);
                }
                catch (ParserException e)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new InterpreterException(e.Message, e, functionCall.Metadata);
                }
            }, function.ReturnType);
    }
}