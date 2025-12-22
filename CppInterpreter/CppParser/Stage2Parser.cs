using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using OneOf;

namespace CppInterpreter.CppParser;

public record struct Default();

[GenerateOneOf]
public partial class Stage2Statement : OneOfBase<AstExpression, Stage2VarDefinition>
{
    
}

[GenerateOneOf]
public partial class Stage2VarDefinition : OneOfBase<AstAssignment, Default>
{
    
} 


public record Stage2SyntaxTree(Stage2Statement[] Statement, Scope<ICppValueBase> Scope, Scope<ICppType> TypeScope);


/// <summary>
/// Parse global functions and variables into scope
/// </summary>
public static class Stage2Parser
{


    public static Scope<ICppType> CreateBaseScope() => new Scope<ICppType>();
    

    public static Stage2SyntaxTree ParseProgram(Stage1SymbolTree program, Scope<ICppValueBase> scope)
    {
        return new Stage2SyntaxTree(
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

        var value = type.Create();
        
        if (!scope.TryBindSymbol(definition.Ident.Value, value))
            throw new Exception($"'{definition.Ident.Value}' was already defined");

        if (definition.Initializer is not null)
            return new AstAssignment(definition.Ident, definition.Initializer);
        return new Default();
    }
}