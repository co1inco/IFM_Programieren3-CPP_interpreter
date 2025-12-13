namespace CppInterpreter;

public class Ast
{

    public class AstProgram
    {
    
    }

    public class AstStatement
    {
    
    }

    public class AstExpression
    {
    
    }

    public record VarDefinition(TypeIdentifier Type, Identifier Value);

    public record struct TypeIdentifier(string Ident, bool IsReference);

    public record struct Identifier(string Value);

}

