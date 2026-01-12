using System.Collections.Immutable;
using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using OneOf;
using OneOf.Types;

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


public static class Stage3StatementParser
{
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
            e => Stage3ExpressionParser.ParseExpression(e, scope).ToStatement(),
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
                Stage3ExpressionParser.ParseExpression(x.Condition, scope),
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
        var condition = Stage3ExpressionParser.ParseExpression(whileStatement.Condition, scope);
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
            : Stage3ExpressionParser.ParseExpression(returnStmt.ReturnValue, scope);

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
            var refValue = Stage3ExpressionParser.ParseExpression(definition.Initializer, scope);
            
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
            : Stage3ExpressionParser.ParseAssignment(new AstAssignment(
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
            : Stage3ExpressionParser.ParseAssignment(
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
}