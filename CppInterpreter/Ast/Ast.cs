using OneOf;

namespace CppInterpreter.Ast;

public class AstProgram
{
    
}

public class AstStatement
{
    
}


public record AstBinOp(AstExpression Left, AstExpression Right, AstBinOpOperator Operator);


public record AstUnary(AstExpression Expression, AstUnary.UnaryOperator Operator)
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
    
public record AstAssignment(AstIdentifier Target, AstExpression Value);
    
public record AstVarDefinition(AstTypeIdentifier AstType, AstIdentifier Value);

public record struct AstTypeIdentifier(string Ident, bool IsReference);

public record struct AstIdentifier(string Value);

public record struct AstAtom(string Value);

[GenerateOneOf]
public partial class AstExpression : OneOfBase<
    AstLiteral, 
    Ast.AstAtom, 
    Ast.AstAssignment,
    Ast.AstBinOp,
    Ast.AstUnary>
{
    
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
