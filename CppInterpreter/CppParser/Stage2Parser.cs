using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;
using OneOf;
using OneOf.Types;

namespace CppInterpreter.CppParser;

public record struct Default();

[GenerateOneOf]
public partial class Stage2Statement : OneOfBase<
    AstExpression, 
    Stage2VarDefinition,
    Stage2FuncDefinition,
    AstStatement,
    None
>
{
    
}

// [GenerateOneOf]
// public partial class Stage2VarDefinition : OneOfBase<AstAssignment, Default>
// {
//     
// } 

public record Stage2VarDefinition(ICppType Type, string Name, AstExpression? Initializer);
public record Stage2FuncDefinition(
    string Name, 
    ICppType ReturnType, 
    CppFunctionParameter[] Arguments,
    AstBlock Body,
    CppUserFunction Function,
    Scope<ICppValue> Closure
);


public record Stage2SymbolTree(Stage2Statement[] Statement, Scope<ICppValue> Scope, Scope<ICppType> TypeScope);


/// <summary>
/// Parse global functions and variables into scope
/// </summary>
public static class Stage2Parser
{


    public static Scope<ICppValue> CreateBaseScope(TextWriter? stdOut = null)
    {
        stdOut ??= Console.Out;
        
        var scope = new Scope<ICppValue>();

        var print = new CppCallableValue(scope);
        scope.TryBindSymbol("print", print);
        
        print.AddOverload(new CppAction<CppInt32Value>("print", stdOut.WriteLine));
        print.AddOverload(new CppAction<CppInt64ValueT>("print", stdOut.WriteLine));
        print.AddOverload(new CppAction<CppBoolValue>("print", stdOut.WriteLine));
        print.AddOverload(new CppAction<CppStringValue>("print", stdOut.WriteLine));
        print.AddOverload(new CppAction<CppCharValueT>("print", stdOut.WriteLine));
        
        scope.BindFunction(new CppAction<CppInt32Value>("print_int", stdOut.WriteLine));
        scope.BindFunction(new CppAction<CppInt64ValueT>("print_long", stdOut.WriteLine));
        scope.BindFunction(new CppAction<CppBoolValue>("print_bool", b => stdOut.WriteLine(b.Value ? "1" : "0" )));
        scope.BindFunction(new CppAction<CppStringValue>("print_string", s => stdOut.WriteLine(s.Value)));
        scope.BindFunction(new CppAction<CppCharValueT>("print_char", stdOut.WriteLine));
        
        return scope;
    }


    public static Stage2SymbolTree ParseProgram(Stage1SymbolTree program, Scope<ICppValue> scope)
    {
        // TODO: TopLevel statements must be collected and initialization must happen before any user code is executed
        return new Stage2SymbolTree(
            program.Statements
                .Select(x => ParseStatement(x, scope, program.Scope))
                .ToArray(),
            scope,
            program.Scope
        );
    }

    public static Stage2Statement ParseReplStatement(Stage1Statement statement, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        if (statement.TryPickT1(out _, out var astStmt))
            return new None();
        
        if (astStmt.TryPickT0(out var expr, out _))
            return expr;

        if (astStmt.TryPickT2(out var func, out _))
            return ParseFuncDefinition(func, scope, typeScope);

        return astStmt;
    }

    public static Stage2Statement ParseStatement(AstStatement statement, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        return statement.Match<Stage2Statement>(
            e => e, // TODO: I think expressions should not be allowed here
            v => ParseVarDefinition(v, scope, typeScope),
            f => ParseFuncDefinition(f, scope, typeScope),
            b => throw b.CreateException("Unsupported top level statement"),
            r => throw r.CreateException("Unsupported top level statement"),
            i => throw i.CreateException("Unsupported top level statement"),
            w => throw w.CreateException("Unsupported top level statement"),
            w => throw w.CreateException("Unsupported top level statement"),
            w => throw w.CreateException("Unsupported top level statement"),
            c => throw new Exception("Class def in stage2. Should have ben handled in stage1")
        );
    }

    public static Stage2VarDefinition ParseVarDefinition(
        AstVarDefinition definition, 
        Scope<ICppValue> scope,
        Scope<ICppType> typeScope)
    {
        if (!typeScope.TryGetSymbol(definition.Type.Ident, out var type))
            definition.Type.ThrowNotFound();

        // Maybe don't use the values here but instead add a ValueReference type?
        var value = type.Create();

        if (!scope.TryBindSymbol(definition.Ident.Value, value))
            definition.Ident.Throw($"'{definition.Ident.Value}' was already defined");

        return new Stage2VarDefinition(type, definition.Ident.Value, definition.Initializer);
    }
    

    public static Stage2FuncDefinition ParseFuncDefinition(
        AstFuncDefinition definition,
        Scope<ICppValue> scope,
        Scope<ICppType> typeScope)
    {
        if (!typeScope.TryGetSymbol(definition.ReturnType.Ident, out var returnType))
            definition.ReturnType.ThrowNotFound();

        if (definition.ReturnType.IsReference)
            definition.ReturnType.Throw($"Return type '{definition.ReturnType.Ident}' is a reference type");
        
        List<CppFunctionParameter> arguments = [];
        foreach (var argument in definition.Arguments)
        {
            // TODO: implement reference types
            if (!typeScope.TryGetSymbol(argument.Type.Ident, out var argumentType))
                argument.Type.ThrowNotFound();
                
            arguments.Add(new CppFunctionParameter(argument.Ident.Value, argumentType, argument.Type.IsReference));
        }

        var function = new CppUserFunction(
            definition.Ident.Value, 
            returnType, 
            arguments.ToArray(),
            definition.Body
        );
        
        if (!scope.TryBindFunction(definition.Ident.Value, function))
            definition.Ident.Throw($"Failed to bind function '{definition.Ident.Value}' to environment");
        
        return new Stage2FuncDefinition(
            definition.Ident.Value,
            returnType,
            arguments.ToArray(),
            definition.Body,
            function,
            scope
        );
    }
}