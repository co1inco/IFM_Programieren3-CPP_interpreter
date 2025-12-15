using System.Linq.Expressions;
using OneOf;

namespace CppInterpreter;

public class Ast
{

    public class AstProgram
    {
    
    }

    public class AstStatement
    {
    
    }


    public record BinOp(AstExpression Left, AstExpression Right, AstBinOp Operator);
    

    public enum UnaryOperator
    {
        Negate,
        BitwiseNot,
        Increment,
        Decrement,
        Positive,
        Negative
    }
    
    public record Unary(AstExpression Expression, UnaryOperator Operator);
    
    public record Assignment(Identifier Target, AstExpression Value);
    
    public record VarDefinition(TypeIdentifier Type, Identifier Value);

    public record struct TypeIdentifier(string Ident, bool IsReference);

    public record struct Identifier(string Value);

    public record struct Atom(string Value);

}

[GenerateOneOf]
public partial class AstExpression : OneOfBase<
    AstLiteral, 
    Ast.Atom, 
    Ast.Assignment,
    Ast.BinOp,
    Ast.Unary>
{
    
}


[GenerateOneOf]
public partial class AstBinOp : OneOfBase<
    AstBinOp.Equatable,
    AstBinOp.Comparable,
    AstBinOp.IntegerOp,
    AstBinOp.BoolOp,
    AstBinOp.Arithmetic
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
        Modulo
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
    }
}

[GenerateOneOf]
public partial class AstLiteral : OneOfBase<char, int, string, bool>
{
        
}
