using CppInterpreter.Ast;
using CppInterpreter.Interpreter;

namespace CppInterpreter.CppParser;

public class P1Statement
{
    
}


public class CppParser
{

    public static void ParseProgram(AstProgram program)
    {
        throw new NotImplementedException();
    }

    public static ICppExpression ParseExpression(AstExpression expression)
    {
        return expression.Match(
            ParseLiteral,
            _ => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
            ParseBinOp,
            _ => throw new NotImplementedException()
        );
    }


    public static ICppExpression ParseBinOp(AstBinOp op)
    {
        var function = op.Operator.Match(
            e => e switch
            {
                AstBinOpOperator.Equatable.Equal => "==",
                AstBinOpOperator.Equatable.NotEqual => "!=",
                _ => throw new ArgumentOutOfRangeException(nameof(e), e, null)
            },
            c => c switch
            {
                AstBinOpOperator.Comparable.LessThan => "<",
                AstBinOpOperator.Comparable.LessThanOrEqual => "<=",
                AstBinOpOperator.Comparable.GreaterThan => ">",
                AstBinOpOperator.Comparable.GreaterThanOrEqual => ">=",
                _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
            },
            i => i switch
            {
                AstBinOpOperator.IntegerOp.BitAnd => "&",
                AstBinOpOperator.IntegerOp.BitOr => "|",
                AstBinOpOperator.IntegerOp.BitXor => "^",
                _ => throw new ArgumentOutOfRangeException(nameof(i), i, null)
            },
            b => b switch
            {
                AstBinOpOperator.BoolOp.And => "&&",
                AstBinOpOperator.BoolOp.Or => "||",
                _ => throw new ArgumentOutOfRangeException(nameof(b), b, null)
            },
            a => a switch
            {
                AstBinOpOperator.Arithmetic.Add => "+",
                AstBinOpOperator.Arithmetic.Subtract => "-",
                AstBinOpOperator.Arithmetic.Multiply => "*",
                AstBinOpOperator.Arithmetic.Divide => "/",
                AstBinOpOperator.Arithmetic.Modulo => "%",
                _ => throw new ArgumentOutOfRangeException(nameof(a), a, null)
            }
        );
        
        return new CppBinOpExpression(
            ParseExpression(op.Left),
            ParseExpression(op.Right),
            $"operator{function}");
    }
    
    public static ICppExpression ParseLiteral(AstLiteral literal) => new CppLiteralExpression(
        literal.Match(
            c => throw new NotImplementedException(),
            i => new CppInt32Value(i),
            i => throw new NotImplementedException(),
            i => throw new NotImplementedException()
        ));
}