using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using OneOf.Types;

namespace CppInterpreter.CppParser;

public class Stage1ParseTree
{
    
}

public class CppStage1Scope
{
    public required Scope<ICppValueBase> Values { get; init; }
    public required Scope<ICppType> Types { get; init; }
}

public interface ICppStatement
{
    void Evaluate();
}

public class CppNoneStatement : ICppStatement
{
    public void Evaluate() {}
}

public class CppExpressionStatement(ICppExpression expression) : ICppStatement 
{
    public void Evaluate()
    {
        expression.Evaluate();
    }
}

public class CppDefinitionStatement() : ICppStatement
{
    public void Evaluate()
    {
        throw new NotImplementedException();
    }
}



public interface ICppStage1Builder
{
    ICppStatement BuildStatement(CppStage1Scope scope);
}

public class CppStage1VarDefinition(AstVarDefinition varDefinition) : ICppStage1Builder
{
    
    public ICppStatement BuildStatement(CppStage1Scope scope)
    {
        if (!scope.Types.TryGetSymbol(varDefinition.AstType.Ident, out var type))
            throw new Exception($"Type not found '{type}'");

        var value = type.Construct();
        
        if (!scope.Values.TryBindSymbol(varDefinition.AstType.Ident, value))
            throw new Exception($"Value '{varDefinition.AstType.Ident}' was already defined");
        

        return new CppNoneStatement();
    }
}

public class CppStage1Expression(AstExpression expression) : ICppStage1Builder
{
    public ICppStatement BuildStatement(CppStage1Scope scope)
    {
        throw new NotImplementedException();
    }
}