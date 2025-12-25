using System.Diagnostics;
using System.Globalization;
using Antlr4.Runtime;
using static Language.GrammarParser;

namespace CppInterpreter.Ast;

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

    public static AstProgram ParseProgram(ProgramContext ctx) => 
        new (
            ctx.topLevelStatement()
                .Select(ParseTopLevelStatement)
                .ToArray()
        );

    public static AstStatement ParseTopLevelStatement(TopLevelStatementContext ctx)
    {
        if (ctx.functionDefinition() is { } funcDef)
            return ParseFunctionDefinition(funcDef);
        if (ctx.variableDefinition() is { } varDef)
            return ParseVarDefinition(varDef);
        throw new Exception("Unsupported top level statement");
    }
    
    public static AstStatement ParseStatement(StatementContext ctx)
    {
        if (ctx.expression() is {} expr)
            return ParseExpression(expr);
        if (ctx.variableDefinition() is {} varDef)
            return ParseVarDefinition(varDef);
        if (ctx.functionDefinition() is {} funcDef)
            return ParseFunctionDefinition(funcDef);
        throw new NotImplementedException();
    }
    
    public static AstExpression ParseExpression(ExpressionContext ctx)
    {
        if (ctx.brace is {} expr) return ParseExpression(expr);
        if (ctx.literal() is { } literal) return ParseLiteral(literal);
        if (ctx.atom() is { } atom) return new AstAtom(atom.GetText());
        if (ctx.assignment() is { } assignment) return ParseAssignment(assignment);
        if (ctx is { left: { } left, right: { } right })
        {
            if (ctx.logic is { } logic) return ParseLogicBinOp(left, right, logic);
            if (ctx.bit is { } bit) return ParseBitBinOp(left, right, bit);
            if (ctx.comp is { } comp) return ParseCompareBinOp(left, right, comp);
            if (ctx.binop is { } ar) return ParseArithmeticBinOp(left, right, ar);
            throw new AntlrMissMatchException("Got left and right but no supported operator");
        }
        if (ctx.func is { } func) return ParseFunctionCall(func, ctx.funcParameters());
        throw new NotImplementedException();
    }

    public static AstBinOp ParseLogicBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "&&" => AstBinOpOperator.BoolOp.And,
            "||" => AstBinOpOperator.BoolOp.Or,
            _ => throw new AntlrMissMatchException($"Invalid  logic operator '{logicOperator.Text}'")
        });

    public static AstBinOp ParseBitBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "|" => AstBinOpOperator.IntegerOp.BitOr,
            "&" => AstBinOpOperator.IntegerOp.BitAnd,
            "^" => AstBinOpOperator.IntegerOp.BitXor,
            _ => throw new AntlrMissMatchException($"Invalid bit operator '{logicOperator.Text}'")
        }); 

    public static AstBinOp ParseCompareBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "==" => AstBinOpOperator.Equatable.Equal,
            "!=" => AstBinOpOperator.Equatable.NotEqual,
            "<" => AstBinOpOperator.Comparable.LessThan,
            "<=" => AstBinOpOperator.Comparable.LessThanOrEqual,
            ">" => AstBinOpOperator.Comparable.GreaterThan,
            ">=" => AstBinOpOperator.Comparable.GreaterThanOrEqual,
            _ => throw new AntlrMissMatchException($"Invalid comparison operator '{logicOperator.Text}'")
        }); 
    
    public static AstBinOp ParseArithmeticBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "+" => AstBinOpOperator.Arithmetic.Add,
            "-" => AstBinOpOperator.Arithmetic.Subtract,
            "*" => AstBinOpOperator.Arithmetic.Multiply,
            "/" => AstBinOpOperator.Arithmetic.Divide,
            "%" => AstBinOpOperator.Arithmetic.Modulo,
            _ => throw new AntlrMissMatchException($"Invalid arithmetic operator '{logicOperator.Text}'")
        }); 
    
    public static AstAssignment ParseAssignment(AssignmentContext ctx) =>
        new(
            ParseVarIdentifier(ctx.varIdentifier()),
            ParseExpression(ctx.expression())
        );

    public static AstFunctionCall ParseFunctionCall(ExpressionContext function, FuncParametersContext? arguments) =>
        new(
            ParseExpression(function),
            arguments?.expression().Select(ParseExpression).ToArray() ?? []
        );

    public static AstVarDefinition ParseVarDefinition(VariableDefinitionContext ctx) =>
        new(
            ParseTypeUsage(ctx.typeIdentifierUsage()),
            ParseVarIdentifier(ctx.varIdentifier()),
            ctx.expression() is not null ? ParseExpression(ctx.expression()) : null
        );

    public static AstFuncDefinition ParseFunctionDefinition(FunctionDefinitionContext ctx)
    {
        var returnType = ctx.TYPE_VOID() is not null
            ? new AstTypeIdentifier("void", false)
            : ParseTypeUsage(ctx.typeIdentifierUsage());
                
        return new AstFuncDefinition(
            new AstIdentifier(ctx.ident.Text),
            returnType,
            Enumerable.Zip(
                    ctx.parameterList()
                        .typeIdentifierUsage()
                        .Select(ParseTypeUsage),
                    ctx.parameterList()
                        .varIdentifier()
                        .Select(x => new AstIdentifier(x.ident.Text))
                    )
                    .Select(x => new AstFunctionDefinitionParameter(x.Second, x.First))
                .ToArray(),
            ParseBlock(ctx.block())
        );
    }

    public static AstStatement[] ParseBlock(BlockContext ctx) => 
        ctx.statement()
            .Select(ParseStatement)
            .ToArray();
    
    public static AstTypeIdentifier ParseTypeUsage(TypeIdentifierUsageContext ctx)
    {
        if (ctx.typeIdentifier().GetText() == "void")
            throw new ParserException($"'void' can not be used as a variable type");
                
        return new AstTypeIdentifier(
            ctx.typeIdentifier().GetText(),
            ctx.@ref is not null
        );
    }

    public static AstIdentifier ParseVarIdentifier(VarIdentifierContext ctx) =>
        new AstIdentifier(ctx.ident.Text);

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