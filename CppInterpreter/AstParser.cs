using Antlr4.Runtime;
using CSharpFunctionalExtensions;
using Language;

using static CppInterpreter.Ast;
using static Language.GrammarParser;

namespace CppInterpreter;

public class ParserException : Exception
{
    public ParserException(string message) : base(message)
    {
        
    }

    public ParserException(string message, IToken token) : base(message)
    {
        
    }
}


public static class AstParser
{



    public static VarDefinition ParseVarDefinition(VariableDefinitionContext ctx) =>
        new(
            ParseTypeUsage(ctx.typeIdentifierUsage()),
            ParseVarIdentifier(ctx.varIdentifier())
        );

    public static TypeIdentifier ParseTypeUsage(TypeIdentifierUsageContext ctx)
    {
        if (ctx.typeIdentifier().GetText() == "void")
            throw new ParserException($"'void' can not be used as a variable type");
                
        return new TypeIdentifier(
            ctx.typeIdentifier().GetText(),
            ctx.@ref is not null
        );
    }

    public static Identifier ParseVarIdentifier(VarIdentifierContext ctx) =>
        new Identifier(ctx.ident.Text);

}