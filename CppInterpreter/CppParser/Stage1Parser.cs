using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using OneOf;

namespace CppInterpreter.CppParser;



[GenerateOneOf]
public partial class Stage1Symbol : OneOfBase<AstStatement>
{
    
}

public record Stage1SymbolTree(Scope<ICppType> Scope, AstStatement[] Statements);


/// <summary>
/// Parse type definitions into scope
/// </summary>
public class Stage1Parser
{

    public static Scope<ICppType> CreateBaseScope()
    {
        var s = new Scope<ICppType>();

        s.TryBindSymbol("void", CppTypes.Void);
        s.TryBindSymbol("int", CppTypes.Int32);
        s.TryBindSymbol("long", CppTypes.Int64);
        s.TryBindSymbol("string", CppTypes.String);
        
        return s;
    }

    public static Stage1SymbolTree ParseProgram(AstProgram program, Scope<ICppType> scope) => 
        ParseProgram(program.Statements, scope);
    
    public static Stage1SymbolTree ParseProgram(IEnumerable<AstStatement> statements, Scope<ICppType> scope) => 
        new(scope, statements.ToArray());


    public static void ParseAssignment(AstAssignment assignment, Scope<ICppType> scope)
    {
        
    }
    
    
}