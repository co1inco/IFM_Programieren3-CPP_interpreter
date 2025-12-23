using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using OneOf;

namespace CppInterpreter.CppParser;

public record struct Default();

[GenerateOneOf]
public partial class Stage2Statement : OneOfBase<AstExpression, Stage2VarDefinition>
{
    
}

// [GenerateOneOf]
// public partial class Stage2VarDefinition : OneOfBase<AstAssignment, Default>
// {
//     
// } 

public record Stage2VarDefinition(ICppType Type, string Name, AstExpression? Initializer);


public record Stage2SymbolTree(Stage2Statement[] Statement, Scope<ICppValueBase> Scope, Scope<ICppType> TypeScope);


/// <summary>
/// Parse global functions and variables into scope
/// </summary>
public static class Stage2Parser
{


    public static Scope<ICppValueBase> CreateBaseScope()
    {
        var scope = new Scope<ICppValueBase>();

        var print = new CppCallableValue(scope);
        scope.TryBindSymbol("print", print);
        return scope;
    }


    public static Stage2SymbolTree ParseProgram(Stage1SymbolTree program, Scope<ICppValueBase> scope)
    {
        // TODO: TopLevel statements must be collected and initialization must happen before any user code is executed
        return new Stage2SymbolTree(
            program.Statements
                .Select(x => ParseStatement(x, scope, program.Scope))
                .ToArray(),
            scope,
            program.Scope
        );
    }

    public static Stage2Statement ParseStatement(AstStatement statement, Scope<ICppValueBase> scope, Scope<ICppType> typeScope)
    {
        return statement.Match<Stage2Statement>(
            e => e,
            v => ParseVarDefinition(v, scope, typeScope)
        );
    }

    public static Stage2VarDefinition ParseVarDefinition(
        AstVarDefinition definition, 
        Scope<ICppValueBase> scope,
        Scope<ICppType> typeScope)
    {
        if (!typeScope.TryGetSymbol(definition.AstType.Ident, out var type))
            throw new Exception($"Unknown type '{definition.AstType.Ident}'");

        // Maybe don't use the values here but instead add a ValueReference type?
        var value = type.Create();
        
        if (!scope.TryBindSymbol(definition.Ident.Value, value))
            throw new Exception($"'{definition.Ident.Value}' was already defined");

        return new Stage2VarDefinition(type, definition.Ident.Value, definition.Initializer);
    }
}