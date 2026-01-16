using System.Collections.Immutable;
using CppInterpreter.Ast;
using CppInterpreter.Helper;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using OneOf;
using OneOf.Types;

namespace CppInterpreter.CppParser;


public record Stage3Statement(
    InterpreterStatementEval StatementEval,
    // ImmutableArray<(ICppType Type, AstMetadata Return)> ReturnValues
    ImmutableArray<Stage3StatementResult> Results
)
{
    public static implicit operator Stage3Statement(InterpreterStatementEval statementEval) =>
        new Stage3Statement(statementEval, []);
};

[GenerateOneOf]
public partial class Stage3StatementResult : OneOfBase<
    Stage3StatementResult.Return, 
    Stage3StatementResult.Continue, 
    Stage3StatementResult.Break, 
    None
>
{
    public record Return(IAstNode Node, ICppType Type);
    public record struct Continue(IAstNode Node);
    public record struct Break(IAstNode Node);
}

public static class Stage3StatementResultExtensions
{
    extension(IEnumerable<Stage3StatementResult> results)
    {
        public IEnumerable<OneOf<Stage3StatementResult.Return, None>> EnsureNoLoopControl() => results.Select(x => x
            .Match(
                r => (OneOf<Stage3StatementResult.Return, None>)r,
                c => throw c.Node.CreateException("'continue' can only be used inside a loop"),
                b => throw b.Node.CreateException("'continue' can only be used inside a loop"),
                n => n
            ));

        public IEnumerable<Stage3StatementResult> FilterLoopControl()
        {
            foreach (var result in results)
            {
                if (result.TryPickT1(out var c, out var r1)
                    || r1.TryPickT1(out var b, out var rem))
                    continue;
                yield return rem.Match<Stage3StatementResult>(
                    r => r, 
                    n => n
                );
            }
        }

        public IEnumerable<Stage3StatementResult> TakeLoopControls(out IList<OneOf<Stage3StatementResult.Return, None>> returns)
        {
            List<OneOf<Stage3StatementResult.Return, None>> returns1 = [];
            List<Stage3StatementResult> loop = [];
            
            foreach (var result in results)
            {
                result.Switch(
                    r => returns1.Add(r),
                    c => loop.Add(c),
                    b => loop.Add(b),
                    n => returns1.Add(n)
                );
            }
            
            returns = returns1;
            return loop;
        }
    }
}


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


public static class Stage3StatementParser
{
    public static Stage3Statement BuildFunction(Stage2FuncDefinition definition, Scope<ICppValue> sc, Scope<ICppType> typeScope)
    {
        definition.Function.BuildBody(definition.Closure, (body, scope) =>
        {
            var bodyStatement = ParseBlock(body, scope, typeScope, suppressBlockScope: true);

            var returnsVoid = definition.ReturnType.Equals(CppTypes.Void);
            
            foreach (var returnValue in bodyStatement.Results.EnsureNoLoopControl())
            {
                if (returnsVoid)
                {
                    if (returnValue.TryPickT0(out var r, out _) && !r.Type.Equals(CppTypes.Void))
                        throw r.Node.CreateException("'void' function can not return value");
                }
                else
                {
                    if (!returnValue.TryPickT0(out var r, out _)) // no return  ed value
                        throw definition.Body.CreateException($"Function must return value of type '{definition.ReturnType.Name}'");
                    
                    // TODO: for inheritance this must check if the type is assignable
                    if (!r.Type.Equals(definition.ReturnType)) // incorrect return type
                        throw r.Node.CreateException($"Function must return value of type '{definition.ReturnType.Name}'");
                }
            }

            if (!returnsVoid && bodyStatement.Results.Length == 0)
                throw definition.Body.CreateException("Non void function must return a value");
            
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

        return new Stage3Statement(_ => new None(), []);
    }

    public static Stage3Statement ParseStatement(AstStatement statement, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        return statement.Match<Stage3Statement>(
            e => Stage3ExpressionParser.ParseExpression(e, scope).ToStatement(),
            d => ParseVariableDefinition(d, scope, typeScope),
            f => throw f.CreateException("Functions must be placed at top level"),
            b => ParseBlock(b, scope, typeScope),
            r => ParseReturn(r, scope),
            i => ParseIf(i, scope, typeScope),
            w => ParseWhile(w, scope, typeScope),
            b => ParseBreak(b, scope),
            c => ParseContinue(c, scope),
            c => throw c.CreateException("Class def in stage3. Should be handled in stage1"),
            f => ParseFor(f, scope, typeScope)
        );
    }

    public static Stage3Statement ParseIf(AstIf ifStmt, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        var branches = ifStmt.Branches
            .Select(x => (
                Stage3ExpressionParser.ParseExpression(x.Condition, scope),
                ParseBlock(x.Body, scope, typeScope)))
            .ToArray();
        
        var elseBlock = ParseBlock(ifStmt.Else,  scope, typeScope);

        var returnValues = branches
            .Select(x => x.Item2)
            .Append(elseBlock)
            .SelectMany(x => x.Results)
            .ToImmutableArray();

        return new Stage3Statement(
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

    public static Stage3Statement ParseWhile(AstWhile whileStatement, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        var condition = Stage3ExpressionParser.ParseExpression(whileStatement.Condition, scope);
        var body = ParseBlock(whileStatement.Body, scope, typeScope);

        return new Stage3Statement( 
            s =>
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
                        // no handling for continue required
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
                        // no handling for continue required
                    }  
                }

                return new None();
            },
            body.Results.FilterLoopControl()
                .Append(new None()) // "else branch", we can not guarantee that a loop would always hit a return 
                .ToImmutableArray()
        );
    }

    public static Stage3Statement ParseFor(AstFor forStatement, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        var forScope = new Scope<ICppValue>(scope);
        
        // TODO: validate that initializer and incrementor do not return anything (Should be prevented by antlr though)
        var initializer = forStatement.Initializer is null ? null : ParseStatement(forStatement.Initializer, forScope, typeScope);
        var condition = Stage3ExpressionParser.ParseExpression(forStatement.Condition, forScope);
        var incrementor = forStatement.Incrementor is null ? null : ParseStatement(forStatement.Incrementor, forScope, typeScope);
        
        var body = ParseBlock(forStatement.Body, forScope, typeScope);

        return new Stage3Statement(s =>
            {
                s = new Scope<ICppValue>(s);

                initializer?.StatementEval(s);

                while (condition.Eval(s).ToBool())
                {
                    incrementor?.StatementEval(s);
                    
                    var bodyResult = body.StatementEval(s);
                    
                    if (bodyResult.TryPickT0(out var r, out var rem1))
                        return r;
                    if (bodyResult.TryPickT2(out var b, out _))
                        break;
                    // no handling for continue required 
                }

                return new None();
            },
            body.Results.FilterLoopControl()
                .Append(new None()) // "else branch", we can not guarantee that a loop would always hit a return 
                .ToImmutableArray());
    }
    
    
    public static Stage3Statement ParseBreak(AstBreak breakStatement, Scope<ICppValue> scope)
    {
        return new Stage3Statement(_ =>  new InterpreterStatementResult.Break(), []);
    }

    public static Stage3Statement ParseContinue(AstContinue continueStatement, Scope<ICppValue> scope)
    {
        return new Stage3Statement(_ =>  new InterpreterStatementResult.Continue(), []);
    }



    public static Stage3Statement ParseBlock(AstBlock block, Scope<ICppValue> scope, Scope<ICppType> typeScope, bool suppressBlockScope = false)
    {
        var parseScope = suppressBlockScope ? scope : new Scope<ICppValue>(scope);
            
        var statements = block.Statements
            .Select(x => ParseStatement(x, parseScope, typeScope))
            .ToArray();

        // Determine all possible results of this block
        List<Stage3StatementResult> results = [];
        bool allPathsReturn = false;
        foreach (var stmt in statements)
        {
            stmt.Results.ForEach(x => x.Switch(
                r => results.Add(r),
                c => results.Add(c),
                b => results.Add(b),
                _ => {} // filter non returning paths
            ));
            if (stmt.Results.Any() && stmt.Results.All(x => x.IsT0))
            {
                allPathsReturn = true;
                break;
            }
        }
        if (!allPathsReturn)
            results.Add(new None()); // Indicate that this block does not return in all paths
        
        return new Stage3Statement(s =>
            {
                var blockScope = suppressBlockScope ? s : new Scope<ICppValue>(s);

                foreach (var statement in statements)
                {
                    var result = statement.StatementEval(blockScope);
                    if (!result.IsNone)
                        return result;
                }

                return new None();
            }, 
            [..results]);

    }

    public static Stage3Statement ParseReturn(AstReturn returnStmt, Scope<ICppValue> scope)
    {
        var expression = returnStmt.ReturnValue is null
            ? new InterpreterExpressionResult(_ => new CppVoidValue(), CppTypes.Void)
            : Stage3ExpressionParser.ParseExpression(returnStmt.ReturnValue, scope);

        return new Stage3Statement(
            s => new InterpreterStatementResult.Return(expression.Eval(s)),
            [ new Stage3StatementResult.Return(returnStmt, expression.ResultType) ]
        );
    }
    
    public static Stage3Statement ParseVariableDefinition(AstVarDefinition definition, Scope<ICppValue> scope, Scope<ICppType> typeScope)
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
            var refValue = Stage3ExpressionParser.ParseExpression(definition.Initializer, scope);
            
            return new Stage3Statement(s =>
                {
                    var value = refValue.Eval(s);
                
                    if (!s.TryBindSymbol(definition.Ident.Value, value))
                        definition.Ident.Throw($"Variable '{definition.Ident.Value}' was already defined");

                    return new None();
                }, []);
        }
     
        var initializer = definition.Initializer is null
            ? null
            : Stage3ExpressionParser.ParseAssignment(new AstAssignment(
                new AstAtom(definition.Ident.Value, definition.Ident.Metadata), 
                definition.Initializer, 
                AstMetadata.Generated()),
                scope);
        
        return new Stage3Statement(s =>
            {
                var instance = type.Create();
                if (!s.TryBindSymbol(definition.Ident.Value, instance))
                    definition.Ident.Throw($"Variable '{definition.Ident.Value}' was already defined");

                initializer?.Eval(s);
                
                return new None();
            }, []);
    }
    
    public static Stage3Statement ParseVariableDefinition(Stage2VarDefinition definition, Scope<ICppValue> scope)
    {
        var initializer = definition.Initializer is null
            ? null
            : Stage3ExpressionParser.ParseAssignment(
                new AstAssignment(
                    new AstAtom(definition.Name, AstMetadata.Generated()), 
                    definition.Initializer, 
                    AstMetadata.Generated()), 
                scope);
        
        if (!scope.TryBindSymbol(definition.Name, definition.Type.Create()))
            throw new Exception($"Variable '{definition.Name}' was already defined");
        
        // TODO: Stage2VarDefinition creation should happen in stage 2
        return new Stage3Statement(s =>
        {
            var instance = definition.Type.Create();
            if (!s.TryBindSymbol(definition.Name, instance))
                throw new Exception($"Variable '{definition.Name}' was already defined");

            _ = initializer?.Eval(s);
            
            return new None();
        }, []);
    }
}