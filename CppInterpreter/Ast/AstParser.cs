using System.Diagnostics;
using System.Globalization;
using Antlr4.Runtime;
using CppInterpreter.CppParser;
using CSharpFunctionalExtensions;
using Language;
using static Language.GrammarParser;


namespace CppInterpreter.Ast;

/// <summary>
/// Exception that indicates a missmatch between the Antlr grammar and AstParser.
/// The AstParser should normaly accept all states the Antlr grammar allows.
/// </summary>
/// <param name="symbol"></param>
/// <param name="message"></param>
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

public class ParserErrorToExceptionListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        throw new ParserException(msg, new AstMetadata(new SourceSymbol(offendingSymbol.Text, line, charPositionInLine)));
    }
}


public static class AstParser
{
    public static void FailOnParserError(this GrammarParser parser)
    {
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ParserErrorToExceptionListener());
    }
    

    public static AstProgram ParseProgram(ProgramContext ctx) => new(
            ctx.topLevelStatement()
                .Select(ParseTopLevelStatement)
                .ToArray(),
            ctx
        );

    public static AstStatement ParseTopLevelStatement(TopLevelStatementContext ctx)
    {
        if (ctx.functionDefinition() is { } funcDef)
            return ParseFunctionDefinition(funcDef);
        if (ctx.variableDefinition() is { } varDef)
            return ParseVarDefinition(varDef);
        if (ctx.@class() is { } classDef)
            return ParseCompoundTypeDefinition(classDef);
        
        throw new UnexpectedAntlrStateException(ctx, "Unknown top level statement variation");
    }

    public static AstStatement ParseStatement(StatementContext ctx)
    {
        if (ctx.expression() is { } expr)
            return ParseExpression(expr);
        if (ctx.variableDefinition() is { } varDef)
            return ParseVarDefinition(varDef);
        if (ctx.functionDefinition() is { } funcDef)
            return ParseFunctionDefinition(funcDef);
        if (ctx.ifStmt() is { } ifStmt)
            return ParseIf(ifStmt);
        if (ctx.whileStmt() is { } whileStmt)
            return ParseWhile(whileStmt);
        if (ctx.doWhileStmt() is { } doWhileStmt)
            return ParseDoWhile(doWhileStmt);
        if (ctx.returnStmt() is { } returnStmt)
            return ParseReturn(returnStmt);
        if (ctx.block() is { } block)
            return ParseBlock(block);
        if (ctx.breakStmt() is { } breakStmt)
            return ParseBreak(breakStmt);
        if (ctx.continueStmt() is { } continueStmt)
            return ParseContinue(continueStmt);
        throw new UnexpectedAntlrStateException(ctx, "Unknown statement variation");
    }
    
    public static AstExpression ParseExpression(ExpressionContext ctx)
    {
        if (ctx.brace is {} expr) return ParseExpression(expr);
        if (ctx.literal() is { } literal) return ParseLiteral(literal);
        if (ctx.atom() is { } atom) return ParseAtom(atom);
        // if (ctx.assignment() is { } assignment) return ParseAssignment(assignment);
        if (ctx is { left: { } left, right: { } right })
        {
            if (ctx.logic is { } logic) return ParseLogicBinOp(left, right, logic);
            if (ctx.bit is { } bit) return ParseBitBinOp(left, right, bit);
            if (ctx.comp is { } comp) return ParseCompareBinOp(left, right, comp);
            if (ctx.binop is { } ar) return ParseArithmeticBinOp(left, right, ar);
            if (ctx.assign is { } assign) return ParseAssignment(left, right); 
            throw new UnexpectedAntlrStateException(ctx, "Got left and right but no supported operator");
        }
        if (ctx.func is { } func) return ParseFunctionCall(func, ctx.funcParameters());
        if (ctx.unary is { } unary) return ParseUnaryOp(ctx.expression()[0], unary);
        if (ctx.suffix is { } suffix) return ParseSuffixOp(ctx.expression()[0], suffix);
        
        throw new UnexpectedAntlrStateException(ctx, "Unknown expression variation");
    }

    public static AstAtom ParseAtom(AtomContext ctx) => new AstAtom(ctx.GetText(), ctx);
    
    public static AstBinOp ParseLogicBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "&&" => AstBinOpOperator.BoolOp.And,
            "||" => AstBinOpOperator.BoolOp.Or,
            _ => throw new UnexpectedAntlrStateException(logicOperator, $"Invalid  logic operator")
        }, AstMetadata.FromToken(logicOperator));

    public static AstBinOp ParseBitBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "|" => AstBinOpOperator.IntegerOp.BitOr,
            "&" => AstBinOpOperator.IntegerOp.BitAnd,
            "^" => AstBinOpOperator.IntegerOp.BitXor,
            _ => throw new UnexpectedAntlrStateException(logicOperator, $"Invalid bit operator")
        }, AstMetadata.FromToken(logicOperator)); 

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
        }, AstMetadata.FromToken(logicOperator)); 
    
    public static AstBinOp ParseArithmeticBinOp(ExpressionContext left, ExpressionContext right, IToken logicOperator) =>
        new(ParseExpression(left), ParseExpression(right), logicOperator.Text switch
        {
            "+" => AstBinOpOperator.Arithmetic.Add,
            "-" => AstBinOpOperator.Arithmetic.Subtract,
            "*" => AstBinOpOperator.Arithmetic.Multiply,
            "/" => AstBinOpOperator.Arithmetic.Divide,
            "%" => AstBinOpOperator.Arithmetic.Modulo,
            _ => throw new UnexpectedAntlrStateException(logicOperator, $"Invalid arithmetic operator")
        }, AstMetadata.FromToken(logicOperator));

    public static AstAssignment ParseAssignment(ExpressionContext left, ExpressionContext right) => new(
        ParseExpression(left),
        ParseExpression(right),
        left
    );
    
    // public static AstAssignment ParseAssignment(AssignmentContext ctx) => 
    //     new(
    //         ParseVarIdentifier(ctx.varIdentifier()),
    //         ParseExpression(ctx.expression()),
    //         ctx
    //     );

    public static AstUnary ParseUnaryOp(ExpressionContext expression, IToken unary) =>
        new(
            ParseExpression(expression),
            unary.Text switch
            {
                "++" => AstUnary.UnaryOperator.Increment,
                "--" => AstUnary.UnaryOperator.Decrement,
                "+" => AstUnary.UnaryOperator.Positive,
                "-" => AstUnary.UnaryOperator.Negative,
                "!" => AstUnary.UnaryOperator.Negate,
                "~" => AstUnary.UnaryOperator.BitwiseNot,
                var t => throw new UnexpectedAntlrStateException(unary, $"Invalid unary operator: '{t}'")
            },
            new AstMetadata(SourceSymbol.Create(unary))
        );

    public static AstSuffix ParseSuffixOp(ExpressionContext ctx, IToken suffix) =>
        new(
            ParseExpression(ctx),
            suffix.Text switch
            {
                "++" => new AstOperator("++"),
                "--" => new AstOperator("--"),
                var t => throw new UnexpectedAntlrStateException(suffix, $"Invalid suffix operator: '{t}'")
            },
            new AstMetadata(SourceSymbol.Create(suffix))
        ); 
    
    public static AstFunctionCall ParseFunctionCall(ExpressionContext function, FuncParametersContext? arguments) =>
        new(
            ParseExpression(function),
            arguments?.expression().Select(ParseExpression).ToArray() ?? [],
            function
        );

    public static AstVarDefinition ParseVarDefinition(VariableDefinitionContext ctx) =>
        new(
            ParseTypeUsage(ctx.typeIdentifierUsage()),
            ParseVarIdentifier(ctx.varIdentifier()),
            ctx.expression() is not null ? ParseExpression(ctx.expression()) : null,
            ctx
        );

    public static AstFuncDefinition ParseFunctionDefinition(FunctionDefinitionContext ctx) 
    {
        var returnType = ctx.TYPE_VOID() is not null
            ? new AstTypeIdentifier("void", false, ctx)
            : ParseTypeUsage(ctx.typeIdentifierUsage());

        return new AstFuncDefinition(
            new AstIdentifier(ctx.ident.Text, ctx),
            returnType,
            ParseFunctionParameters(ctx.parameterList()),
            ParseBlock(ctx.block()),
            ctx
        );
    }

    public static AstFunctionDefinitionParameter[] ParseFunctionParameters(ParameterListContext ctx) =>
        Enumerable.Zip(
                ctx.typeIdentifierUsage()
                    .Select(ParseTypeUsage),
                ctx.varIdentifier()
                    .Select(x => new AstIdentifier(x.ident.Text, AstMetadata.FromToken(x.ident)))
            )
            .Select(x => new AstFunctionDefinitionParameter(x.Second, x.First, ctx))
            .ToArray();
    
    public static AstCompoundTypeDefinition ParseCompoundTypeDefinition(ClassContext ctx)
    {
        var name = ParseIdentifier(ctx.typeIdentifier());
        var baseClasses = ctx.classInheritance() is { } b
            ? b.classInheitanceIdent().Select(ParseBaseClassIdentifier).ToArray()
            : [];
        
        var kind = ctx.defaultVis.Text switch
        {
            "class" => AstCompoundTypeDefinition.TypeKind.Class,
            "struct" => AstCompoundTypeDefinition.TypeKind.Struct,
            _ => throw new UnexpectedAntlrStateException(ctx.defaultVis, "Unexpected TypeDef type")
        };
        
        List<AstCompoundTypeMember<AstVarDefinition>> variables = [];
        List<AstCompoundTypeMember<AstFuncDefinition>> functions = [];
        List<AstCompoundTypeMember<AstFuncDefinition>> constructors = [];
        AstFuncDefinition? destructor = null;
        
        var currentVisibility = kind switch
        {
            AstCompoundTypeDefinition.TypeKind.Class => AstVisibility.Private,
            AstCompoundTypeDefinition.TypeKind.Struct => AstVisibility.Public,
            _ => throw new ArgumentOutOfRangeException()
        };

        foreach (var member in ctx.classBlock().classBlockStatement())
        {
            if (member.pub is not null)
            {
                currentVisibility = AstVisibility.Public;
            }
            else if (member.prv is not null)
            {
                currentVisibility = AstVisibility.Public;
            }
            else if (member.classConstructor() is { } ctor)
            {
                if (ctor.ident.Text != name.Value)
                    throw new ParserException("Constructor must have the same name as the containing class", ctor);
                
                constructors.Add(new AstCompoundTypeMember<AstFuncDefinition>(
                    new AstFuncDefinition(
                        new AstIdentifier("$ctor", member),
                        new  AstTypeIdentifier("void", false, ctx),
                        ParseFunctionParameters(ctor.parameterList()),
                        ParseBlock(ctor.block()),
                        member
                    ),
                    currentVisibility,
                    member
                    ));
            }
            else if (member.classDestructor() is { } dtor)
            {
                if (dtor.ident.Text != name.Value)
                    throw new ParserException("Destructor must have the same name as the containing class", dtor);
                
                if (destructor is not null)
                    throw new ParserException("Multiple constructors defined", member);

                destructor = new AstFuncDefinition(
                    new AstIdentifier("$dtor", member),
                    new AstTypeIdentifier("void", false, ctx),
                    [],
                    ParseBlock(dtor.block()),
                    member
                );
            }
            else if (member.functionDefinition() is { } fd)
            {
                functions.Add(new AstCompoundTypeMember<AstFuncDefinition>(
                    ParseFunctionDefinition(fd),
                    currentVisibility,
                    member
                ));   
            }
            else if (member.variableDefinition() is { } vd)
            {
                variables.Add(new AstCompoundTypeMember<AstVarDefinition>(
                    ParseVarDefinition(vd),
                    currentVisibility,
                    member
                ));
            }
            else
            {
                throw new UnexpectedAntlrStateException(member, "Unknown class member kind");
            }
        }
        
        return new AstCompoundTypeDefinition(
            name,
            baseClasses,
            functions.ToArray(),
            variables.ToArray(),
            kind,
            constructors.ToArray(),
            destructor,
            ctx
        );
    }
    
    public static AstCompoundTypeMember<AstIdentifier> ParseBaseClassIdentifier(ClassInheitanceIdentContext ctx) => new(
        ParseIdentifier(ctx.typeIdentifier()),
        ParseVisibility(ctx.vis),    
        ctx
    );
      
    public static AstVisibility ParseVisibility(IToken token) => token?.Text switch
    {
        "private" => AstVisibility.Private,
        "public" => AstVisibility.Public,
        "protected" => AstVisibility.Protected,
        null => AstVisibility.Private, 
        var t => throw new UnexpectedAntlrStateException(token, $"Invalid visibility: '{t}'")
    };
    
    public static AstIdentifier ParseIdentifier(TypeIdentifierContext ctx) => new(
        ctx.GetText(),
        ctx
    );

    public static AstIdentifier ParseIdentifier(IToken identToken) => new(
        identToken.Text,
        new AstMetadata(SourceSymbol.Create(identToken)) 
    );
    
    public static AstIf ParseIf(IfStmtContext ctx)
    {
        List<(AstExpression, AstBlock)> branches = [];

        branches.Add((
            ParseExpression(ctx.cond),
            ParseInnerBlock(ctx.innerBlock())
        ));
        
        ElseStmtContext? context = ctx.elseStmt();
        while (context?.ifStmt() is not null)
        {
            branches.Add((
                ParseExpression(context.ifStmt().cond), 
                ParseInnerBlock(context.ifStmt().innerBlock())
            ));

            context = context.ifStmt().elseStmt();
        }

        var elseBlock = context?.innerBlock() is null
            ? new AstBlock([], ctx)
            : ParseInnerBlock(context?.innerBlock()!);

        return new AstIf(branches.ToArray(), elseBlock, ctx);
    }

    public static AstWhile ParseWhile(WhileStmtContext ctx) => new AstWhile(
        ParseExpression(ctx.cond),
        ParseInnerBlock(ctx.innerBlock()),
        false,
        ctx);

    public static AstWhile ParseDoWhile(DoWhileStmtContext ctx) => new AstWhile(
        ParseExpression(ctx.cond),
        ParseBlock(ctx.block()),
        true,
        ctx);

    public static AstBreak ParseBreak(BreakStmtContext ctx) => new AstBreak(ctx);
    public static AstContinue ParseContinue(ContinueStmtContext ctx) => new AstContinue(ctx);
    
    public static AstBlock ParseBlock(BlockContext ctx) => 
        new AstBlock(
            ctx.statement()
                .Select(ParseStatement)
                .ToArray(),
            ctx
        );

    public static AstBlock ParseInnerBlock(InnerBlockContext ctx)
    {
        if (ctx.block() is {} block)
            return ParseBlock(block);
        if (ctx.statement() is { } statement)
            return new AstBlock([ ParseStatement(statement) ], ctx);
        return new AstBlock([], ctx);
    }
    
    public static AstReturn ParseReturn(ReturnStmtContext ctx) => 
        new (
            ctx.expression() is null ? null : ParseExpression(ctx.expression()),
            ctx
        );
    
    public static AstTypeIdentifier ParseTypeUsage(TypeIdentifierUsageContext ctx)
    {
        if (ctx.typeIdentifier().GetText() == "void")
            throw new ParserException("'void' can not be used here", new AstMetadata(SourceSymbol.Create(ctx)));
                        
        return new AstTypeIdentifier(
            ctx.typeIdentifier().GetText(),
            ctx.@ref is not null,
            ctx
        );
    }

    public static AstIdentifier ParseVarIdentifier(VarIdentifierContext ctx) =>
        new AstIdentifier(ctx.ident.Text, ctx);

    public static AstLiteral ParseLiteral(LiteralContext ctx)
    {
        if (ctx.@char is { } c) return char.Parse(c.Text.Trim('\''));
        if (ctx.intLiteral() is { } i) return ParseIntLiteral(i);
        if (ctx.@bool is { } b) return bool.Parse(b.Text);
        if (ctx.str is { } s) return s.Text.Trim('"');
        throw new UnexpectedAntlrStateException(ctx, $"Invalid literal");
    }

    public static int ParseIntLiteral(IntLiteralContext ctx)
    {
        if (ctx.@int is { } dec) return int.Parse(dec.Text);
        if (ctx.bin is { } bin) return int.Parse(bin.Text.Replace("0b", "").Replace("_", ""), NumberStyles.AllowBinarySpecifier);
        if (ctx.hex is { } hex) return int.Parse(hex.Text.Replace("0x", ""), NumberStyles.HexNumber);
        throw new UnreachableException("Unsupported number style");
    }


}