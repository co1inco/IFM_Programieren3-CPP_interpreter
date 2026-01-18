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




public static class Stage3Parser
{

    public static Stage3Statement ParseProgram(Stage2SymbolTree program, Scope<ICppValue> scope)
    {
        var statements = program.Statement
            .Select(x => x.Match(
                e => Stage3ExpressionParser.ParseExpression(e, scope).ToStatement(),
                v => Stage3StatementParser.ParseVariableDefinition(v, scope),
                f => Stage3StatementParser.ParseStage2FunctionDefinition(f, scope, program.TypeScope),
                s => throw s.CreateException("Statement can not be top level"),
                none => null!,
                c => ParseCompoundTypeDefinition(c, program.TypeScope)
            ))
            .Where(x => x is not null)
            .ToArray();

        return new Stage3Statement(s =>
        {
            foreach (var statement in statements)
            {
                statement.StatementEval(s);
            }

            return new None();
        }, []);
    }

    public static Stage3Statement ParseReplStatement(Stage2Statement statement, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        if (statement.TryPickT4(out var none, out var stmt))
            return new Stage3Statement(_ => new None(), []);
        
        var parsedStatement = stmt.Match(
            e => Stage3ExpressionParser.ParseExpression(e, scope).ToStatement(),
            v => Stage3StatementParser.ParseVariableDefinition(v, scope),
            f => Stage3StatementParser.ParseStage2FunctionDefinition(f, scope, typeScope),
            s => Stage3StatementParser.ParseStatement(s, scope, typeScope),
            c => ParseCompoundTypeDefinition(c, typeScope)
        );
        
        return new Stage3Statement(s =>
        {
            parsedStatement.StatementEval(s);
            return new None();
        }, []);
    }


    public static Stage3Statement ParseCompoundTypeDefinition(Stage2CompoundTypeDefinition typeDefinition, Scope<ICppType> typeScope)
    {
        typeDefinition.Type.BuildMemberFunctions(() => null!);

        foreach (var (func, closure) in typeDefinition.Functions)
        {
            func.Function.BuildBody(func.Body, closure, typeScope, Stage3StatementParser.BuildFunction);
        }
        
        return new Stage3Statement(
            s => new None(), 
            []
        );
    }
    
}