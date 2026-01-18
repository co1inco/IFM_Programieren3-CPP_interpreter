using System.Reflection;
using System.Runtime.CompilerServices;
using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.CppParser;


public record Stage2CompoundTypeDefinition(
    CppUserType Type,
    (Stage2FuncDefinition Func, Scope<ICppValue> Closure)[] Functions    
);

public class UserTypeBuilderContext(
    CppUserType typeDefinition,
    ICppUserTypeMemberBuilder builder,
    Scope<ICppValue> namespaceScope, 
    // Scope<ICppValue> instanceParseScope,
    Scope<ICppType> typeScope)
{
    private readonly List<ICppFunction> _functions = [];
    
    public List<Stage2FuncDefinition> FunctionsToInitialize { get; } = [];

    public CppUserType Type => typeDefinition;
    
    public Scope<ICppValue> NamespaceScope => namespaceScope;
    public Scope<ICppValue> InstanceParserScope => Type.CreateParserScope();
    public Scope<ICppType>  TypeScope => typeScope;
    

    public IEnumerable<CppUserType.MemberValue> AddedVariables => builder.Variables;
    
    public IEnumerable<ICppFunction> AddedConstructors => builder.Constructors;

    public IEnumerable<ICppFunction> RegisteredFunctions => _functions;

    public void AddVariable(string name, ICppType type, InterpreterExpressionResult? initializer, MemberVisibility visibility)
    {
        builder.AddVariable(name, type, initializer, visibility);
    }

    public void AddFunction(string name, Stage2FuncDefinition function, MemberVisibility visibility)
    {
        builder.AddFunction(name, function.Function, visibility);
        
        FunctionsToInitialize.Add(function);
        _functions.Add(function.Function);
    }
    
    public void AddFunction(string name, ICppFunction function, MemberVisibility visibility)
    {
        builder.AddFunction(name, function, visibility);
        _functions.Add(function);
    }
    
    /// <summary>
    /// Register a new type constructor 
    /// </summary>
    /// <param name="constructor">A function that sets up a new instance, initializes members and calls the user defined constructor</param>
    /// <param name="userFunction"></param>
    /// <param name="visibility"></param>
    public bool AddConstructor(ICppFunction constructor, Stage2FuncDefinition? userFunction, MemberVisibility visibility)
    {
        if (userFunction is not null)
            FunctionsToInitialize.Add(userFunction);
                
        builder.AddConstructor(constructor);
        
        if (visibility == MemberVisibility.Public)
            return namespaceScope.BindFunction(constructor, typeDefinition.Name);
        return true;
    }

    public void SetDestructor(Stage2FuncDefinition userFunction)
    {
        builder.SetDestructor(userFunction.Function);
        FunctionsToInitialize.Add(userFunction);
    }
}

public static class Stage2UserTypeParser
{
    public static Stage2CompoundTypeDefinition ParseCompoundTypeDefinition(
        Stage1CompoundTypeDefinition typeDef, 
        Scope<ICppValue> namespaceScope, 
        Scope<ICppType> typeScope)
    {
        List<(Stage2FuncDefinition Func, Scope<ICppValue> Closure)> functionsToInitialize = [];    
        
        typeDef.Type.BuildMembers(namespaceScope, (b, valueScope) =>
        {
            // TODO: check for duplicate definitions
            var builder = new UserTypeBuilderContext(
                typeDef.Type,
                b,
                namespaceScope,
                typeScope
            );
            
            ParseMemberVariables(typeDef.Ast.Variables, builder);
            
            ParseMemberFunctions(typeDef.Ast.Functions, builder);
            
            ParseConstructors(typeDef.Ast.Constructors, builder);
            
            ParseDestructor(typeDef.Ast.Destructor, builder);
            
            functionsToInitialize.AddRange(builder.FunctionsToInitialize.Select(x => (x, valueScope)));
        });

        return new Stage2CompoundTypeDefinition(typeDef.Type, functionsToInitialize.ToArray());
    }

    public static void ParseMemberVariables(
        IEnumerable<AstCompoundTypeMember<AstVarDefinition>> varDefinitions,
        UserTypeBuilderContext builder)
    {
        foreach (var variable in varDefinitions)
        {
            var (name, type, initializer) = ParseMemberVariable(
                variable.Member,
                builder.NamespaceScope, 
                builder.TypeScope
            );
            
            builder.AddVariable(name, type, initializer, variable.Visibility.ToMemberVisibility());
        }
    }
    
    public static (string Name, ICppType Type, InterpreterExpressionResult? Initializer) ParseMemberVariable(
        AstVarDefinition varDefinition,
        Scope<ICppValue> valueScope,
        Scope<ICppType> typeScope
        )
    {
        if (!typeScope.TryGetSymbol(varDefinition.Type.Ident, out var type))
            throw varDefinition.Type.CreateException("Unknown type");

        //Note: usage of s3 parser here. Could be removed by only allowing const values
        //TODO: this should possibly only support const values (ie. atoms)
        var initializer = varDefinition.Initializer is null
            ? null
            : Stage3ExpressionParser.ParseExpression(varDefinition.Initializer, valueScope);

        return (varDefinition.Ident.Value, type, initializer);
    }


    public static void ParseMemberFunctions(
        IEnumerable<AstCompoundTypeMember<AstFuncDefinition>> functions,
        UserTypeBuilderContext builder)
    {
        foreach (var function in functions)
        {
            //TODO: might be required to share a single instance parse scope
            var s2Function = Stage2Parser.ParseFuncDefinition(
                function.Member,
                builder.Type,
                builder.InstanceParserScope, 
                builder.TypeScope
            );
            
            builder.AddFunction(s2Function.Function.Name, s2Function, function.Visibility.ToMemberVisibility());
        }
            
        if (!HasAssignmentOperator(builder.RegisteredFunctions, builder.Type))
        {
            var f = new DefaultAssignmentOperator(builder.Type);
            builder.AddFunction(f.Name, f, MemberVisibility.Public);
        }
    }
    
    
    public static void ParseConstructors(
        ICollection<AstCompoundTypeMember<AstFuncDefinition>> userConstructors,
        UserTypeBuilderContext builder)
    {
        var membersToInitialize = builder.AddedVariables
            .Select(x => (
                x.MemberInfo.Name,
                x.Initializer))
            .Where(x => x.Initializer is not null)
            .ToArray();
            
        
        foreach (var constructor in userConstructors)
        {
            ParseConstructor(
                builder,
                constructor.Member,
                constructor.Visibility.ToMemberVisibility(),
                membersToInitialize!
            );
        }
        
        if (builder.AddedConstructors.All(x => x.ParameterTypes.Length != 0))
        {
            // If no constructors, add auto generated constructor
            // If any constructor, add private constructor to generate "uninitialized" instance
            //  TODO: better handling of the later one? 
            var visibility = userConstructors.Count == 0
                ? MemberVisibility.Public
                : MemberVisibility.Private;
            
            ParseConstructor(
                builder,
                null,
                visibility,
                membersToInitialize!
            );
        }

        if (!HasCopyConstructor(builder.AddedConstructors, builder.Type))
        {
            builder.AddConstructor(new DefaultCopyConstructor(builder.Type), null, MemberVisibility.Public);
        }
    }
    
    public static void ParseConstructor(
        UserTypeBuilderContext builderContext,
        AstFuncDefinition? userFunctionAst,
        MemberVisibility visibility,
        IEnumerable<(string Name, InterpreterExpressionResult InitialValue)> initializers)
    {
        var userFunction = userFunctionAst is null ? null 
            : Stage2Parser.ParseFuncDefinition(
                userFunctionAst, 
                builderContext.Type,
                builderContext.InstanceParserScope,
                builderContext.TypeScope
            );
        // functionsToInitialize.Add((userFunction.Function, userTypeParseScope));
                
        var constructor = new BaseUserTypeConstructor(
            builderContext.Type,
            builderContext.NamespaceScope,
            initializers,
            userFunction?.Function
        );

        if (!builderContext.AddConstructor(constructor, userFunction, visibility))
            if (userFunctionAst is not null)
                throw userFunctionAst.CreateException("Failed to bind constructor");
    }

    public static void ParseDestructor(AstFuncDefinition? destructorFunction, UserTypeBuilderContext builder)
    {
        if (destructorFunction is null)
            return;
        
        var destructor = Stage2Parser.ParseFuncDefinition(
            destructorFunction, 
            builder.Type,
            builder.InstanceParserScope,
            builder.TypeScope
        );
        
        builder.SetDestructor(destructor);
    }
    
    
    public static bool HasCopyConstructor(IEnumerable<ICppFunction> functions, ICppType instanceType) => 
        functions.Any(x => x.ParameterTypes is [{ IsReference: true } p] && p.Type.Equals(instanceType));

    public static bool HasAssignmentOperator(IEnumerable<ICppFunction> functions, ICppType instanceType) =>
        functions.Any(function =>
            function is
            {
                Name: "operator=", 
                ParameterTypes: [{ IsReference: true } p]
            } 
            && p.Type.Equals(instanceType)
        );
    
    public static MemberVisibility ToMemberVisibility(this AstVisibility visibility) => visibility switch
    {
        AstVisibility.Public => MemberVisibility.Public,
        AstVisibility.Private => MemberVisibility.Private,
        AstVisibility.Protected => MemberVisibility.Protected,
        _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
    };
} 