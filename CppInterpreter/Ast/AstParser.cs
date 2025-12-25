using System.Diagnostics;
using System.Globalization;
using Antlr4.Runtime;
using static Language.GrammarParser;


namespace CppInterpreter.Ast;

public class AstParserException(SourceSymbol symbol, string message) : Exception($"{message}: {symbol.Text}")
{
    public AstParserException(ParserRuleContext ctx, string message) : this(SourceSymbol.Create(ctx), message) { }
    public AstParserException(IToken token, string message) : this(SourceSymbol.Create(token), message) { }

}

public sealed class UnexpectedAntlrStateException(SourceSymbol symbol, string message = "Unsupported Antlr context") 
    : Exception($"{message}: {symbol.Text}")
{
    public UnexpectedAntlrStateException(ParserRuleContext context, string message = "Unsupported Antlr context") 
        : this(SourceSymbol.Create(context), message) { }
    
    public UnexpectedAntlrStateException(IToken token, string message = "Unsupported Antlr context") 
        : this(SourceSymbol.Create(token), message) { }
    
    
    public SourceSymbol Symbol { get; } = symbol;

    public string ContextText => Symbol.Text;
}

public record SourceSymbol(string Text, int Line, int Column)
{
    public static SourceSymbol Create(ParserRuleContext ctx)
    {
        return new SourceSymbol(ctx.GetText(), ctx.Start.Line, ctx.Start.Column);
    }
    
    public static SourceSymbol Create(IToken token)
    {
        return new SourceSymbol(token.Text, token.Line, token.Column);
    }
};


public static class AstParser
{

    private static AstSymbol<T> Parse<T>(ParserRuleContext ctx, Func<T> func) =>
        new(
            func(),
            SourceSymbol.Create(ctx)
        );

    public static AstSymbol<AstProgram> ParseProgram(ProgramContext ctx) => Parse(ctx, () => 
        new AstProgram(
            ctx.topLevelStatement()
                .Select(ParseTopLevelStatement)
                .ToArray()
        ));

    public static AstSymbol<AstStatement> ParseTopLevelStatement(TopLevelStatementContext ctx) => Parse<AstStatement>(ctx, () =>
    {
        if (ctx.functionDefinition() is { } funcDef)
            return ParseFunctionDefinition(funcDef).Symbol;
        if (ctx.variableDefinition() is { } varDef)
            return ParseVarDefinition(varDef).Symbol;

        throw new UnexpectedAntlrStateException(ctx, "Unknown top level statement variation");
    });

    public static AstSymbol<AstStatement> ParseStatement(StatementContext ctx) => Parse<AstStatement>(ctx, () =>
    {
        if (ctx.expression() is { } expr)
            return ParseExpression(expr); // TODO remove
        if (ctx.variableDefinition() is { } varDef)
            return ParseVarDefinition(varDef).Symbol;
        if (ctx.functionDefinition() is { } funcDef)
            return ParseFunctionDefinition(funcDef).Symbol;
        if (ctx.ifStmt() is { } ifStmt)
            throw new NotImplementedException("If statement");
        if (ctx.whileStmt() is { } whileStmt)
            throw new NotImplementedException("while statement");
        if (ctx.doWhileStmt() is { } doWhileStmt)
            throw new NotImplementedException("doWhile statement");

        throw new UnexpectedAntlrStateException(ctx, "Unknown statement variation");
    });
    
    public static AstExpression ParseExpression(ExpressionContext ctx)
    {
        if (ctx.brace is {} expr) return ParseExpression(expr);
        if (ctx.literal() is { } literal) return ParseLiteral(literal);
        if (ctx.atom() is { } atom) return ParseAtom(atom);
        if (ctx.assignment() is { } assignment) return ParseAssignment(assignment);
        if (ctx is { left: { } left, right: { } right })
        {
            if (ctx.logic is { } logic) return Parse(ctx, () => ParseLogicBinOp(left, right, logic));
            if (ctx.bit is { } bit) return Parse(ctx, () => ParseBitBinOp(left, right, bit));
            if (ctx.comp is { } comp) return Parse(ctx, () => ParseCompareBinOp(left, right, comp));
            if (ctx.binop is { } ar) return Parse(ctx, () => ParseArithmeticBinOp(left, right, ar));
            throw new UnexpectedAntlrStateException(ctx, "Got left and right but no supported operator");
        }
        if (ctx.func is { } func) return ParseFunctionCall(func, ctx.funcParameters());

        throw new UnexpectedAntlrStateException(ctx, "Unknown expression variation");
    }

    public static AstSymbol<AstAtom> ParseAtom(AtomContext ctx) => Parse(ctx, () => new AstAtom(ctx.GetText()));
    
    public static AstBinOp ParseLogicBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "&&" => AstBinOpOperator.BoolOp.And,
            "||" => AstBinOpOperator.BoolOp.Or,
            _ => throw new UnexpectedAntlrStateException(logicOperator, $"Invalid  logic operator")
        });

    public static AstBinOp ParseBitBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "|" => AstBinOpOperator.IntegerOp.BitOr,
            "&" => AstBinOpOperator.IntegerOp.BitAnd,
            "^" => AstBinOpOperator.IntegerOp.BitXor,
            _ => throw new UnexpectedAntlrStateException(logicOperator, $"Invalid bit operator")
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
            _ => throw new UnexpectedAntlrStateException(logicOperator, $"Invalid comparison operator")
        }); 
    
    public static AstBinOp ParseArithmeticBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "+" => AstBinOpOperator.Arithmetic.Add,
            "-" => AstBinOpOperator.Arithmetic.Subtract,
            "*" => AstBinOpOperator.Arithmetic.Multiply,
            "/" => AstBinOpOperator.Arithmetic.Divide,
            "%" => AstBinOpOperator.Arithmetic.Modulo,
            _ => throw new UnexpectedAntlrStateException(logicOperator, $"Invalid arithmetic operator")
        }); 
    
    public static AstSymbol<AstAssignment> ParseAssignment(AssignmentContext ctx) => Parse(ctx, () => 
        new AstAssignment(
            ParseVarIdentifier(ctx.varIdentifier()),
            ParseExpression(ctx.expression())
        ));

    public static AstSymbol<AstFunctionCall> ParseFunctionCall(ExpressionContext function, FuncParametersContext? arguments) => Parse(function, () =>
        new AstFunctionCall(
            ParseExpression(function),
            arguments?.expression().Select(ParseExpression).ToArray() ?? []
        ));

    public static AstSymbol<AstVarDefinition> ParseVarDefinition(VariableDefinitionContext ctx) => Parse(ctx, () =>
        new AstVarDefinition(
            ParseTypeUsage(ctx.typeIdentifierUsage()),
            ParseVarIdentifier(ctx.varIdentifier()),
            ctx.expression() is not null ? ParseExpression(ctx.expression()) : null
        ));

    public static AstSymbol<AstFuncDefinition> ParseFunctionDefinition(FunctionDefinitionContext ctx) => Parse(ctx,
        () =>
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
        });

    public static AstSymbol<AstStatement>[] ParseBlock(BlockContext ctx) => 
        ctx.statement()
            .Select(ParseStatement)
            .ToArray();
    
    public static AstTypeIdentifier ParseTypeUsage(TypeIdentifierUsageContext ctx)
    {
        if (ctx.typeIdentifier().GetText() == "void")
            throw new AstParserException(ctx, $"Type can not be used here");
                
        return new AstTypeIdentifier(
            ctx.typeIdentifier().GetText(),
            ctx.@ref is not null
        );
    }

    public static AstIdentifier ParseVarIdentifier(VarIdentifierContext ctx) =>
        new AstIdentifier(ctx.ident.Text);

    public static AstSymbol<AstLiteral> ParseLiteral(LiteralContext ctx) => Parse<AstLiteral>(ctx, () =>
    {
        if (ctx.@char is { } c) return char.Parse(c.Text.Trim('\''));
        if (ctx.intLiteral() is { } i) return ParseIntLiteral(i);
        if (ctx.@bool is { } b) return bool.Parse(b.Text);
        if (ctx.str is { } s) return s.Text.Trim('"');
        throw new AstParserException(ctx, $"Invalid literal");
    });

    public static int ParseIntLiteral(IntLiteralContext ctx)
    {
        if (ctx.@int is { } dec) return int.Parse(dec.Text);
        if (ctx.bin is { } bin) return int.Parse(bin.Text.Replace("0b", "").Replace("_", ""), NumberStyles.AllowBinarySpecifier);
        if (ctx.hex is { } hex) return int.Parse(hex.Text.Replace("0x", ""), NumberStyles.HexNumber);
        throw new UnreachableException("Unsupported number style");
    }

    
}