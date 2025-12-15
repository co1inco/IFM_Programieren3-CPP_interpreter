using System.Diagnostics;
using System.Globalization;
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

    public static AstExpression ParseExpression(ExpressionContext ctx)
    {
        throw new NotImplementedException();
    }
    
    public static Ast.Assignment ParseAssignment(AssignmentContext ctx)
    {
        throw new NotImplementedException();
    }


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

    public static AstLiteral ParseLiteral(LiteralContext ctx)
    {
        if (ctx.@char is { } c) return  char.Parse(c.Text.Trim('\''));
        if (ctx.intLiteral() is { } i) return ParseIntLiteral(i);
        if (ctx.@bool is { } b) return bool.Parse(b.Text);
        if (ctx.str is { } s) return s.Text.Trim('"');
        throw new ParserException($"'{ctx.str}' can not be used as a literal");
    }

    public static int ParseIntLiteral(IntLiteralContext ctx)
    {
        if (ctx.@int is { } dec) return int.Parse(dec.Text);
        if (ctx.bin is { } bin) return int.Parse(bin.Text.Replace("0b", "").Replace("_", ""), NumberStyles.AllowBinarySpecifier);
        if (ctx.hex is { } hex) return int.Parse(hex.Text.Replace("0x", ""), NumberStyles.HexNumber);
        throw new UnreachableException("Unsupported number style");
    }

    
}