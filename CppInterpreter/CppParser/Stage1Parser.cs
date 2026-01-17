using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using OneOf;
using OneOf.Types;

namespace CppInterpreter.CppParser;


[GenerateOneOf]
public partial class Stage1Symbol : OneOfBase<
    AstStatement,
    Stage1CompoundTypeDefinition
>
{
    
}

public record Stage1SymbolTree(Scope<ICppType> Scope, Stage1Symbol[] Statements);

[GenerateOneOf]
public partial class Stage1Statement : OneOfBase<AstStatement, None> {}

public record Stage1CompoundTypeDefinition(
    AstCompoundTypeDefinition Ast,
    CppUserType Type
);


/// <summary>
/// Parse type definitions into scope
/// </summary>
public class Stage1Parser
{

    public static Scope<ICppType> CreateBaseScope()
    {
        var s = new Scope<ICppType>();

        s.TryBindSymbol("void", CppTypes.Void);
        s.TryBindSymbol("char", CppTypes.Char);
        s.TryBindSymbol("int", CppTypes.Int32);
        s.TryBindSymbol("long", CppTypes.Int64);
        s.TryBindSymbol("string", CppTypes.String);
        s.TryBindSymbol("bool", CppTypes.Boolean);
        
        return s;
    }

    public static Stage1SymbolTree ParseProgram(AstProgram program, Scope<ICppType> scope) => 
        ParseProgram(program.Statements, scope);
    
    public static Stage1SymbolTree ParseProgram(IEnumerable<AstStatement> statements, Scope<ICppType> scope) => 
        new(
            scope, 
            statements.Select(Stage1Symbol (x) =>
                {
                    if (x.TryPickT9(out AstCompoundTypeDefinition compound, out _))
                        return ParseCompoundTypeDefinition(compound, scope);
                    return ParseStatement(x, scope);
                })
            .ToArray());

    public static Stage1Statement ParseRepl(AstStatement statement, Scope<ICppType> scope) =>
        statement;

    public static Stage1CompoundTypeDefinition ParseCompoundTypeDefinition(AstCompoundTypeDefinition typeDefinition, Scope<ICppType> scope)
    {
        var userType = new CppUserType(typeDefinition.Ident.Value);
        scope.TryBindSymbol(userType.Name, userType);
        
        return new Stage1CompoundTypeDefinition(
            typeDefinition,
            userType
        );
    }

    public static AstStatement ParseStatement(AstStatement statement, Scope<ICppType> scope) => statement;

}