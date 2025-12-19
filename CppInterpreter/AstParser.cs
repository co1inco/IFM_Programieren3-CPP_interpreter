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

public sealed class AntlrMissMatchException(string message) : Exception(message); 


public static class AstParser
{

    public static AstExpression ParseExpression(ExpressionContext ctx)
    {
        if (ctx.literal() is { } literal) return ParseLiteral(literal);
        if (ctx.atom() is { } atom) return new Atom(atom.GetText());
        if (ctx.assignment() is { } assignment) return ParseAssignment(assignment);
        if (ctx is { left: { } left, right: { } right })
        {
            if (ctx.logic is { } logic) return ParseLogicBinOp(left, right, logic);
            if (ctx.bit is { } bit) return ParseLogicBinOp(left, right, bit);
            if (ctx.comp is { } comp) return ParseLogicBinOp(left, right, comp);
            if (ctx.binop is { } ar) return ParseArithmeticBinOp(left, right, ar);
            throw new AntlrMissMatchException("Got left and right but no supported operator");
        }
        throw new NotImplementedException();
    }

    public static Ast.BinOp ParseLogicBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "&&" => AstBinOp.BoolOp.And,
            "||" => AstBinOp.BoolOp.Or,
            _ => throw new AntlrMissMatchException($"Invalid  logic operator '{logicOperator.Text}'")
        });

    public static Ast.BinOp ParseBitBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "|" => AstBinOp.IntegerOp.BitOr,
            "&" => AstBinOp.IntegerOp.BitAnd,
            "^" => AstBinOp.IntegerOp.BitXor,
            _ => throw new AntlrMissMatchException($"Invalid bit operator '{logicOperator.Text}'")
        }); 

    public static Ast.BinOp ParseCompareBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "==" => AstBinOp.Equatable.Equal,
            "!=" => AstBinOp.Equatable.NotEqual,
            "<" => AstBinOp.Comparable.LessThan,
            "<=" => AstBinOp.Comparable.LessThanOrEqual,
            ">" => AstBinOp.Comparable.GreaterThan,
            ">=" => AstBinOp.Comparable.GreaterThanOrEqual,
            _ => throw new AntlrMissMatchException($"Invalid comparison operator '{logicOperator.Text}'")
        }); 
    
    public static Ast.BinOp ParseArithmeticBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "+" => AstBinOp.Arithmetic.Add,
            "-" => AstBinOp.Arithmetic.Subtract,
            "*" => AstBinOp.Arithmetic.Multiply,
            "/" => AstBinOp.Arithmetic.Divide,
            "%" => AstBinOp.Arithmetic.Modulo,
            _ => throw new AntlrMissMatchException($"Invalid arithmetic operator '{logicOperator.Text}'")
        }); 
    
    // public static Ast.BinOp Parse
    
    
    public static Ast.Assignment ParseAssignment(AssignmentContext ctx) =>
        new(
            ParseVarIdentifier(ctx.varIdentifier()),
            ParseExpression(ctx.expression())
        );

    

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