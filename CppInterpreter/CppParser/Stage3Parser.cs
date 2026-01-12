using System.Collections.Immutable;
using System.Diagnostics;
using CppInterpreter.Ast;
using CppInterpreter.Helper;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using CSharpFunctionalExtensions;
using OneOf;
using OneOf.Types;
using Maybe = CSharpFunctionalExtensions.Maybe;

namespace CppInterpreter.CppParser;


public record InterpreterStatement(
    InterpreterStatementEval StatementEval,
    ImmutableArray<(ICppType Type, AstMetadata Return)> ReturnValues
)
{
    public static implicit operator InterpreterStatement(InterpreterStatementEval statementEval) =>
        new InterpreterStatement(statementEval, []);
};

public delegate InterpreterStatementResult InterpreterStatementEval(Scope<ICppValue> scope);

[GenerateOneOf]
public partial class InterpreterStatementResult : OneOfBase<
    InterpreterStatementResult.Return,
    InterpreterStatementResult.Continue,
    InterpreterStatementResult.Break,
    None
>
{
    public record struct Return(ICppValue Value);
    public record struct Continue(IAstNode ContinueNode);
    public record struct Break(IAstNode ContinueNode);

    public bool IsContinue => IsT1;
    public bool IsBreak => IsT2;
    public bool IsNone => IsT3;

    public OneOf<Return, None> ThrowIfLoopControl()
    {
        if (TryPickT1(out var c, out var rem1))
            c.ContinueNode.Throw("Can only be used in a loop construct");
        if (rem1.TryPickT1(out var b, out var rem2))
            b.ContinueNode.Throw("Can only be used in a loop construct");
        return rem2;
    }
}


public record InterpreterExpressionResult(
    InterpreterExpressionEval Eval,
    ICppType ResultType)
{
    public InterpreterStatement ToStatement() => new InterpreterStatement(s =>
    {
        _ = Eval(s);
        return new None();
    }, []);
}

public delegate ICppValue InterpreterExpressionEval(Scope<ICppValue> scope);

public static class Stage3Parser
{

    public static InterpreterStatement ParseProgram(Stage2SymbolTree program, Scope<ICppValue> scope)
    {
        var statements = program.Statement
            .Select(x => x.Match(
                e => ParseExpression(e, scope).ToStatement(),
                v => ParseVariableDefinition(v, scope),
                f => BuildFunction(f, scope, program.TypeScope),
                s => throw s.CreateException("Statement can not be top level"),
                none => null!
            ))
            .Where(x => x is not null)
            .ToArray();

        return new InterpreterStatement(s =>
        {
            foreach (var statement in statements)
            {
                statement.StatementEval(s);
            }

            return new None();
        }, []);
    }

    public static InterpreterStatement ParseReplStatement(Stage2Statement statement, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        if (statement.TryPickT4(out var none, out var stmt))
            return new InterpreterStatement(_ => new None(), []);
        
        var parsedStatement = stmt.Match(
            e => ParseExpression(e, scope).ToStatement(),
            v => ParseVariableDefinition(v, scope),
            f => BuildFunction(f, scope, typeScope),
            s => ParseStatement(s, scope, typeScope)
        );
        
        return new InterpreterStatement(s =>
        {
            parsedStatement.StatementEval(s);
            return new None();
        }, []);
    }
    
    
    public static InterpreterStatement BuildFunction(Stage2FuncDefinition definition, Scope<ICppValue> sc, Scope<ICppType> typeScope)
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
                return bodyStatement.StatementEval(s)
                    .ThrowIfLoopControl()
                    .Match(
                        r => r.Value,
                        n =>
                        {
                            if (!definition.ReturnType.Equals(CppTypes.Void))
                                throw new ParserException("Return statement missing", body.Metadata);
                            return new CppVoidValue();
                        }
                    );
            };
        });

        return new InterpreterStatement(_ => new None(), []);
    }

    public static InterpreterStatement ParseStatement(AstStatement statement, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        return statement.Match<InterpreterStatement>(
            e => ParseExpression(e, scope).ToStatement(),
            d => ParseVariableDefinition(d, scope, typeScope),
            f => throw f.CreateException("Functions must be placed at top level"),
            b => ParseBlock(b, scope, typeScope),
            r => ParseReturn(r, scope),
            i => ParseIf(i, scope, typeScope),
            w => ParseWhile(w, scope, typeScope),
            b => ParseBreak(b, scope),
            c => ParseContinue(c, scope),
            c => throw c.CreateException("Class def in stage3. Should be handled in stage1")
        );
    }

    public static InterpreterStatement ParseIf(AstIf ifStmt, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        var branches = ifStmt.Branches
            .Select(x => (
                ParseExpression(x.Condition, scope),
                ParseBlock(x.Body, scope, typeScope)))
            .ToArray();
        
        var elseBlock = ParseBlock(ifStmt.Else,  scope, typeScope);

        var returnValues = branches
            .Select(x => x.Item2)
            .Append(elseBlock)
            .SelectMany(x => x.ReturnValues)
            .ToImmutableArray();

        return new InterpreterStatement(
            s =>
            {
                foreach (var (condition, body) in branches)
                {
                    var result = condition.Eval(s);
                    if (result.ToBool()) 
                        return body.StatementEval(s);
                }

                return elseBlock.StatementEval(s);
            }, 
            returnValues);
    }

    public static InterpreterStatement ParseWhile(AstWhile whileStatement, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        var condition = ParseExpression(whileStatement.Condition, scope);
        var body = ParseBlock(whileStatement.Body, scope, typeScope);

        return body with { 
            StatementEval = s =>
            {
                if (whileStatement.DoWhile)
                {
                    do
                    {
                        var result = body.StatementEval(s);

                        if (result.TryPickT0(out var r, out var rem1))
                            return r;
                        if (result.TryPickT2(out var b, out _))
                            break;
                    } while (condition.Eval(s).ToBool());
                }
                else
                {
                    while (condition.Eval(s).ToBool())
                    {
                        var result = body.StatementEval(s);
                        if (result.TryPickT0(out var r, out var rem1))
                            return r;
                        if (result.TryPickT2(out var b, out _))
                            break;
                    }  
                }

                return new None();
            } 
        };
    }

    public static InterpreterStatement ParseBreak(AstBreak breakStatement, Scope<ICppValue> scope)
    {
        return new InterpreterStatement(_ =>  new InterpreterStatementResult.Break(), []);
    }

    public static InterpreterStatement ParseContinue(AstContinue continueStatement, Scope<ICppValue> scope)
    {
        return new InterpreterStatement(_ =>  new InterpreterStatementResult.Continue(), []);
    }



    public static InterpreterStatement ParseBlock(AstBlock block, Scope<ICppValue> scope, Scope<ICppType> typeScope, bool suppressBlockScope = false)
    {
        var parseScope = suppressBlockScope ? scope : new Scope<ICppValue>(scope);
            
        var stmt = block.Statements
            .Select(x => ParseStatement(x, parseScope, typeScope))
            .ToArray();

        var returns = stmt.SelectMany(x => x.ReturnValues).ToArray();

        return new InterpreterStatement(s =>
            {
                var blockScope = suppressBlockScope ? s : new Scope<ICppValue>(s);

                foreach (var statement in stmt)
                {
                    var result = statement.StatementEval(blockScope);
                    if (!result.IsNone)
                        return result;
                }

                return new None();
            }, 
            [..returns]);

    }

    public static InterpreterStatement ParseReturn(AstReturn returnStmt, Scope<ICppValue> scope)
    {
        var expression = returnStmt.ReturnValue is null
            ? new InterpreterExpressionResult(_ => new CppVoidValue(), CppTypes.Void)
            : ParseExpression(returnStmt.ReturnValue, scope);

        return new InterpreterStatement(
            s => new InterpreterStatementResult.Return(expression.Eval(s)),
            [ (expression.ResultType, returnStmt.Metadata) ]
        );
    }
    
    public static InterpreterStatement ParseVariableDefinition(AstVarDefinition definition, Scope<ICppValue> scope, Scope<ICppType> typeScope)
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
            
            return new InterpreterStatement(s =>
                {
                    var value = refValue.Eval(s);
                
                    if (!s.TryBindSymbol(definition.Ident.Value, value))
                        definition.Ident.Throw($"Variable '{definition.Ident.Value}' was already defined");

                    return new None();
                }, []);
        }
     
        var initializer = definition.Initializer is null
            ? null
            : ParseAssignment(new AstAssignment(
                new AstAtom(definition.Ident.Value, definition.Ident.Metadata), 
                definition.Initializer, 
                AstMetadata.Generated()),
                scope);
        
        return new InterpreterStatement(s =>
            {
                var instance = type.Create();
                if (!s.TryBindSymbol(definition.Ident.Value, instance))
                    definition.Ident.Throw($"Variable '{definition.Ident.Value}' was already defined");

                initializer?.Eval(s);
                
                return new None();
            }, []);
    }
    
    public static InterpreterStatement ParseVariableDefinition(Stage2VarDefinition definition, Scope<ICppValue> scope)
    {
        var initializer = definition.Initializer is null
            ? null
            : ParseAssignment(
                new AstAssignment(
                    new AstAtom(definition.Name, AstMetadata.Generated()), 
                    definition.Initializer, 
                    AstMetadata.Generated()), 
                scope);
        
        if (!scope.TryBindSymbol(definition.Name, definition.Type.Create()))
            throw new Exception($"Variable '{definition.Name}' was already defined");
        
        // TODO: Stage2VarDefinition creation should happen in stage 2
        return new InterpreterStatement(s =>
        {
            var instance = definition.Type.Create();
            if (!s.TryBindSymbol(definition.Name, instance))
                throw new Exception($"Variable '{definition.Name}' was already defined");

            _ = initializer?.Eval(s);
            
            return new None();
        }, []);
    }

    public static InterpreterExpressionResult ParseAssignmentTargetExpression(AstExpression expression, Scope<ICppValue> scope)
    {
        if (expression.TryPickT1(out var atom, out var rem1))
            return ParseAtom(atom, scope);
        //TODO: member access
        
        throw expression.CreateException("Target of an assignment must be an identifier ior a member accessor");
    }
    
    
    public static InterpreterExpressionResult ParseExpression(AstExpression expression, Scope<ICppValue> scope) =>
        expression.Match<InterpreterExpressionResult>(
            ParseLiteral,
            a => ParseAtom(a, scope),
            a => ParseAssignment(a, scope),
            b => ParseBinOp(b, scope),
            u => ParseUnaryOp(u, scope),
            func => ParseFunctionCall(func, scope),
            s => ParseSuffixOp(s, scope),
            m => ParseMemberAccess(m, scope)
        );

    public static InterpreterExpressionResult ParseAtom(AstAtom atom, Scope<ICppValue> scope)
    {
        if (!scope.TryGetSymbol(atom.Value, out var symbol))
            atom.Throw("Undefined value");
        
        return new InterpreterExpressionResult(
            s =>
            {
                // Should not happen
                if (!s.TryGetSymbol(atom.Value, out var variable))
                    atom.Throw($"Variable not found '{atom.Value}'");

                return variable;
            }, 
            symbol.GetCppType
        );
    }

    public static InterpreterExpressionResult ParseAssignment(AstAssignment assignment, Scope<ICppValue> scope)
    {
        var inner = ParseExpression(assignment.Value, scope);
        var target = ParseAssignmentTargetExpression(assignment.Target, scope);
        // if (!scope.TryGetSymbol(assignment.Target.Value, out var target))
        //     assignment.Throw("Variable is not defined");
        
        if (target.ResultType.Equals(CppTypes.Callable))
            assignment.Throw("Can not assign to callable");

        if (!target.ResultType.Equals(inner.ResultType))
            assignment.Throw($"Incompatible types. Expected '{target.ResultType}' got '{inner.ResultType}'");
        
        
        
        return new InterpreterExpressionResult(
            s =>
            {
                var targetValue = target.Eval(s);
                
                var exprValue = inner.Eval(s);
                
                // TODO: Can this be outside the interpreter or will this interfere with inheritance?
                // TODO: change assignment resolution. The generated (all?) overloads should be resolved to base class assignments 
                var function = targetValue.GetCppType.GetFunction("operator=", CppMemberBindingFlags.PublicInstance);
                if (function is null)
                    throw assignment.CreateException($"Type '{targetValue.GetCppType.Name}' can not be assigned to");
                
                function.Invoke(targetValue, [exprValue]);
                return exprValue;
            },
            target.ResultType
        );

    }
        


    public static InterpreterExpressionResult ParseBinOp(AstBinOp op, Scope<ICppValue> scope)
    {
        var left = ParseExpression(op.Left, scope);
        var right = ParseExpression(op.Right, scope);

        // Special handler for short-circuiting && and || (don't evaluate unnecessary)
        if (op.Operator.TryPickT3(out var boolOp, out var remaining))
        {
            // check if type has an overload for the operator. short-circuit is not supported for custom operators 
            var functionName = $"operator{BoolOpString(boolOp)}";
            
            if (!left.ResultType.TryGetFunctionOverload(functionName, CppMemberBindingFlags.PublicInstance,  [ right.ResultType ], out _))
            {

                return new InterpreterExpressionResult(s =>
                {
                    if (boolOp == AstBinOpOperator.BoolOp.And)
                    {
                        var l = left.Eval(s);
                        if (!l.ToBool())
                            return new CppBoolValue(false);
                        return right.Eval(s);
                    }

                    if (boolOp == AstBinOpOperator.BoolOp.Or)
                    {
                        var l = left.Eval(s);
                        if (l.ToBool())
                            return new CppBoolValue(true);
                        return right.Eval(s);
                    }

                    throw new ArgumentOutOfRangeException($"Invlaid bool operator: {boolOp}");

                }, CppTypes.Boolean);

            }
            
        }
        
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
            BoolOpString,
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
        
        if (!left.ResultType.TryGetFunctionOverload($"operator{function}", CppMemberBindingFlags.PublicInstance, [right.ResultType], out var memberFunc))
            op.Throw($"Type '{left.ResultType}' does not have a matching operator '{function}'");
        
        if (left.ResultType.GetFunction($"operator{function}", CppMemberBindingFlags.PublicInstance) is not {} mb)
            throw op.CreateException($"Type '{left.ResultType}' does not have a matching operator '{function}'");
        
        return new InterpreterExpressionResult(
            s =>
            {
                var l = left.Eval(s);
                var r = right.Eval(s);

                // Getting the function again could help with virtual members later?
                // var f = l.GetCppType.GetMemberFunction($"operator{function}", r.GetCppType);
                // return f.Invoke(l, [r]);
                return mb.Invoke(l, [r]);
            },
            memberFunc.ReturnType
        );

        string BoolOpString(AstBinOpOperator.BoolOp b) => b switch
        {
            AstBinOpOperator.BoolOp.And => "&&",
            AstBinOpOperator.BoolOp.Or => "||",
            _ => throw new ArgumentOutOfRangeException(nameof(b), b, null)
        };
    }

    public static InterpreterExpressionResult ParseUnaryOp(AstUnary unary, Scope<ICppValue> scope)
    {

        var function = unary.Operator switch
        {
            AstUnary.UnaryOperator.Negate => "!",
            AstUnary.UnaryOperator.BitwiseNot => "~",
            AstUnary.UnaryOperator.Increment => "++",
            AstUnary.UnaryOperator.Decrement => "--",
            AstUnary.UnaryOperator.Positive => "+",
            AstUnary.UnaryOperator.Negative => "-",
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var left = ParseExpression(unary.Expression, scope);
        
        if (!left.ResultType.TryGetFunctionOverload($"operator{function}", CppMemberBindingFlags.PublicInstance, [], out var memberFunc))
            unary.Throw($"Type '{left.ResultType}' does not implement unary operator '{function}'");

        return new InterpreterExpressionResult(
            s =>
            {
                var result = left.Eval(s);
                // Getting the function again could help with virtual members later?
                var f = result.GetCppType.GetFunction($"operator{function}", CppMemberBindingFlags.PublicInstance);
                if (f is null)
                    throw new UnreachableException();
                
                return f.Invoke(result, []);
            },
            memberFunc.ReturnType
        );
    }

    public static InterpreterExpressionResult ParseSuffixOp(AstSuffix suffix, Scope<ICppValue> scope)
    {
        var functionName = $"operator{suffix.Operator.Value}";
        
        // Suffix operators expect an additional int parameter to differentiate from the prefix operator  
        var expr = ParseExpression(suffix.Expression, scope);
        // if (!expr.ResultType.TryGetMemberFunction(functionName, out var memberFunc, CppTypes.Int32))
        //     suffix.Throw($"Type '{expr.ResultType}' does not implement suffix operator '{suffix.Operator.Value}'");
        
        if (expr.ResultType.GetFunction(functionName, CppMemberBindingFlags.PublicInstance) is not {} function)
            throw suffix.CreateException($"Type '{expr.ResultType}' does not implement suffix operator '{suffix.Operator.Value}'");
        
        if (function.GetOverload(expr.ResultType, [CppTypes.Int32]) is not  {} overload)
            throw suffix.CreateException($"Type '{expr.ResultType}' does not have overload '{function}'");
        
        return new InterpreterExpressionResult(
            s =>
            {
                var result = expr.Eval(s);
                // Getting the function again could help with virtual members later?
                
                return overload.Invoke(result, [ new CppInt32Value(0) ]);
            },
            overload.ReturnType
        );
    }

    public static InterpreterExpressionResult ParseMemberAccess(AstMemberAccess memberAccess, Scope<ICppValue> scope)
    {
        var value = ParseExpression(memberAccess.Instance, scope);

        var flags = CppMemberBindingFlags.Public | CppMemberBindingFlags.Instance;
        if (value.ResultType.GetMember(memberAccess.Member.Value, flags) is not {} member)
            throw memberAccess.Member.CreateException($"Type '{value.ResultType.Name}'  does not have a member '{memberAccess.Member.Value}'");
        
        return new InterpreterExpressionResult(
            s => member.GetValue(value.Eval(s)),
            member.MemberType
        );
    }
    
    public static InterpreterExpressionResult ParseLiteral(AstLiteral literal) => 
        literal.Match(
            c => new InterpreterExpressionResult(_ => new CppCharValueT(c), CppTypes.Char),
            i => new InterpreterExpressionResult(_ => new CppInt32Value(i), CppTypes.Int32),
            s => new InterpreterExpressionResult(_ => new CppStringValue(s),  CppTypes.String) ,
            b => new InterpreterExpressionResult(_ => new CppBoolValue(b),  CppTypes.Boolean) 
        );

    public static InterpreterExpressionResult ParseFunctionCall(AstFunctionCall functionCall, Scope<ICppValue> scope)
    {
        // TODO: check functionCall type here
        var callable = ParseExpression(functionCall.Function, scope);
        var arguments = functionCall.Arguments
            .Select(x => ParseExpression(x, scope))
            .ToArray();
        
        if (!callable.ResultType.Equals(CppTypes.Callable))
            functionCall.Throw("Symbol is not a function");
        
        if (callable.ResultType is not CppCallableType callableType)
            throw functionCall.CreateException("Symbol is not a function");

        var function = callableType.CallableFunctions.FirstOrDefault(x => x
            .ParametersMatch(arguments.Select(y => y.ResultType)));
        
        if (function is null)
            functionCall.Throw($"No matching overload: [{string.Join(", ", arguments.Select(x => x.ResultType.Name))}]");
        
        //TODO: already have the type of the callable here and validate parameters / get overload
        // ParseExpression should return a dummy value or a type (callables should already know their functions)

        return new InterpreterExpressionResult(
            s =>
            {
                var c = callable.Eval(s);
                var a = arguments
                    .Select(x => x.Eval(s))
                    .ToArray();

                if (c is not CppCallableValue callableValue)
                    throw new InterpreterException($"Expected callable symbol, got '{c.GetCppType}'", functionCall.Metadata);

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