using Antlr4.Runtime;
using Antlr4.Runtime.Tree.Pattern;
using CSharpFunctionalExtensions;
using OneOf;

namespace CppInterpreter.Ast;

// public record AstSymbol<T>(T Symbol, SourceSymbol Source)
// {
//     public static implicit operator T(AstSymbol<T> symbol) => symbol.Symbol; 
//     public static implicit operator AstSymbol<T>(T symbol) => new AstSymbol<T>(symbol, new SourceSymbol("<unknown>", -1, -1)); 
// };

public interface IAstNode
{
    public AstMetadata Metadata { get; }
}

public record AstMetadata(SourceSymbol Source)
{
    public static implicit operator AstMetadata(ParserRuleContext context) => new(SourceSymbol.Create(context));

    public static AstMetadata FromToken(IToken token) => new(SourceSymbol.Create(token));

    public static AstMetadata Generated() => 
        new AstMetadata(new SourceSymbol("Generated", 0, 0));
    public static AstMetadata Generated(string message) => 
        new AstMetadata(new SourceSymbol(message, 0, 0));
}


public record AstProgram(
    AstStatement[] Statements,
    AstMetadata Metadata
) : IAstNode;

[GenerateOneOf]
public partial class AstStatement : OneOfBase<
    AstExpression,
    AstVarDefinition,
    AstFuncDefinition,
    AstBlock,
    AstReturn,
    AstIf,
    AstWhile,
    AstBreak,
    AstContinue
>
{
    
}

public record AstBlock(
    AstStatement[] Statements,
    AstMetadata Metadata
) : IAstNode;

public record AstBinOp(
    AstExpression Left,
    AstExpression Right,
    AstBinOpOperator Operator,
    AstMetadata Metadata
) : IAstNode;


public record AstUnary(
    AstExpression Expression,
    AstUnary.UnaryOperator Operator,
    AstMetadata Metadata
) : IAstNode
{
    public enum UnaryOperator
    {
        Negate,
        BitwiseNot,
        Increment,
        Decrement,
        Positive,
        Negative
    }
}
    
public record AstAssignment(
    AstExpression Target,
    AstExpression Value,
    AstMetadata Metadata
) : IAstNode;
    
public record AstVarDefinition(
    AstTypeIdentifier Type, 
    AstIdentifier Ident, 
    AstExpression? Initializer,
    AstMetadata Metadata
) : IAstNode;

public record AstFuncDefinition(
    AstIdentifier Ident,
    AstTypeIdentifier ReturnType,
    AstFunctionDefinitionParameter[] Arguments,
    AstBlock Body,
    AstMetadata Metadata
) : IAstNode;

public record AstFunctionDefinitionParameter(
    AstIdentifier Ident, 
    AstTypeIdentifier Type,
    AstMetadata Metadata
) : IAstNode;

public record AstTypeIdentifier(
    string Ident,
    bool IsReference,
    AstMetadata Metadata
) : IAstNode;

public record AstIdentifier(
    string Value,
    AstMetadata Metadata
) : IAstNode;

public record AstAtom(
    string Value,
    AstMetadata Metadata
) : IAstNode;

public record AstIf(
    (AstExpression Condition, AstBlock Body)[] Branches, 
    AstBlock Else,
    AstMetadata Metadata
) : IAstNode;

public record AstWhile(
    AstExpression Condition,
    AstBlock Body,
    bool DoWhile,
    AstMetadata Metadata
) : IAstNode;

public record AstBreak(AstMetadata Metadata) : IAstNode;

public record AstContinue(AstMetadata Metadata) : IAstNode;

public record AstReturn(
    AstExpression? ReturnValue, 
    AstMetadata Metadata
) : IAstNode;

[GenerateOneOf]
public partial class AstExpression : OneOfBase<
    AstLiteral, 
    AstAtom, 
    AstAssignment,
    AstBinOp,
    AstUnary,
    AstFunctionCall>, IAstNode
{
    public AstMetadata Metadata => Match(
        x => new AstMetadata(new SourceSymbol("<unknown literal>", 0, 0)),
        x => x.Metadata,
        x => x.Metadata,
        x => x.Metadata,
        x => x.Metadata,
        x => x.Metadata
    );
}

// TODO: The operator is only used to create the the name for the function (eg. operator+) so storing it here as a simple string should be enough
[GenerateOneOf]
public partial class AstBinOpOperator : OneOfBase<
    AstBinOpOperator.Equatable,
    AstBinOpOperator.Comparable,
    AstBinOpOperator.IntegerOp,
    AstBinOpOperator.BoolOp,
    AstBinOpOperator.Arithmetic
>
{
    public enum Equatable
    {
        Equal,
        NotEqual,
    }
    
    public enum Comparable
    {
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
    }

    public enum IntegerOp
    {
        BitAnd,
        BitOr,
        BitXor,
    }

    public enum BoolOp
    {
        And,
        Or
    }

    public enum Arithmetic
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo
        
    }
}

[GenerateOneOf]
public partial class AstLiteral : OneOfBase<char, int, string, bool>
{
        
}

public record AstFunctionCall(
    AstExpression Function,
    AstExpression[] Arguments,
    AstMetadata Metadata
) : IAstNode;


public static class GeneratedAstTreeBuilder 
{
    public static AstIdentifier AstIdentifier(string name, AstMetadata? m = null) => 
        new AstIdentifier(name, m ?? AstMetadata.Generated());
    
    public static AstTypeIdentifier AstTypeIdentifier(string name, bool isReference, AstMetadata? m = null) => 
        new AstTypeIdentifier(name, isReference, m ?? AstMetadata.Generated());

    public static AstFuncDefinition AstFuncDefinition(AstIdentifier ident, AstTypeIdentifier returnType, AstMetadata? m = null) =>
        new AstFuncDefinition(
            ident,
            returnType,
            [],
            new AstBlock([], AstMetadata.Generated()),
            m ?? AstMetadata.Generated()
        );

    public static AstFuncDefinition AstFuncDefinition(
        AstIdentifier ident, 
        AstTypeIdentifier returnType,
        AstFunctionDefinitionParameter[] arguments,
        AstMetadata? m = null) =>
        new AstFuncDefinition(
            ident,
            returnType,
            arguments,
            new AstBlock([], AstMetadata.Generated()),
            m ?? AstMetadata.Generated()
        );
    
    public static AstFuncDefinition AstFuncDefinition(
        AstIdentifier ident, 
        AstTypeIdentifier returnType,
        AstFunctionDefinitionParameter[] arguments,
        AstBlock body,
        AstMetadata? m = null) =>
        new AstFuncDefinition(
            ident,
            returnType,
            arguments,
            body,
            m ?? AstMetadata.Generated()
        );

    public static AstBlock AstBlock(AstStatement[] statements, AstMetadata? m = null) => 
        new(statements, m ?? AstMetadata.Generated());
    
    public static AstFunctionDefinitionParameter AstFunctionDefinitionParameter(AstIdentifier ident, AstTypeIdentifier type, AstMetadata? m = null) => 
        new(
            ident, type, m ?? AstMetadata.Generated()
        );
    
    public static AstVarDefinition AstVarDefinition(AstTypeIdentifier type, AstIdentifier ident, AstExpression? initializer, AstMetadata? m = null) => 
        new (type, ident, initializer, m ?? AstMetadata.Generated());

    public static AstLiteral AstLiteral(int value) => new AstLiteral(value);
    public static AstLiteral AstLiteral(string value) => new AstLiteral(value);
    public static AstLiteral AstLiteral(bool value) => new AstLiteral(value);
    
    public static AstAtom AstAtom(string name, AstMetadata? m = null) => 
        new AstAtom(name, m ?? AstMetadata.Generated($"Atom: {name}"));
    
    public static AstExpression AstFunctionCallExpr(AstExpression expression, AstExpression[] parameters, AstMetadata? m = null) =>
        AstFunctionCall(expression, parameters, m);
    
    public static AstFunctionCall AstFunctionCall(AstExpression expression, AstExpression[] parameters, AstMetadata? m = null) =>
        new (expression, parameters, m ?? AstMetadata.Generated());
    
    public static AstFunctionCall AstFunctionCall(string name, AstExpression[] parameters, AstMetadata? m = null) =>
        new (AstAtom(name), parameters, m ?? AstMetadata.Generated());
    
    public static AstExpression AstAssignmentExpr(AstIdentifier ident, AstExpression value, AstMetadata? m = null) => 
        AstAssignment(ident, value, null);
    public static AstAssignment AstAssignment(AstIdentifier ident, AstExpression value, AstMetadata? m = null) => 
        new AstAssignment(new AstAtom(ident.Value, ident.Metadata), value, m ?? AstMetadata.Generated($"Assignment: {ident.Value}"));
    public static AstAssignment AstAssignment(AstExpression expr, AstExpression value, AstMetadata? m = null) => 
        new AstAssignment(expr, value, m ?? AstMetadata.Generated($"Assignment: {expr.Value}"));
    
    public static AstBinOp AstBinOp(AstExpression left, AstExpression right, AstBinOpOperator op, AstMetadata? m = null) => 
        new(left, right, op, m ?? AstMetadata.Generated());
    
    
    public static AstIf AstIf((AstExpression, AstBlock)[] branches, AstBlock? elseBranch, AstMetadata? m = null) =>
        new AstIf(
            branches,
            elseBranch ?? new AstBlock([], AstMetadata.Generated()),
            AstMetadata.Generated()
        );
    
    public static (AstExpression, AstBlock) AstIfBranch(AstExpression expression, AstBlock block) =>
        (expression, block);
    
    public static (AstExpression, AstBlock) AstIfBranch(AstExpression expression, params AstStatement[] block) =>
        (expression, AstBlock(block));
}