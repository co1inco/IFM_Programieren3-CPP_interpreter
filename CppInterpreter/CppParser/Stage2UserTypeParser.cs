using CppInterpreter.Ast;
using CppInterpreter.Interpreter;
using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.CppParser;


public record Stage2CompoundTypeDefinition(
    CppUserType Type,
    (CppUserFunction Func, Scope<ICppValue> Closure)[] Functions    
);

public class UserTypeBuilderContext(
    CppUserType typeDefinition,
    ICppUserTypeMemberBuilder builder,
    Scope<ICppValue> namespaceScope, 
    // Scope<ICppValue> instanceParseScope,
    Scope<ICppType> typeScope)
{
    public List<CppUserFunction> FunctionsToInitialize { get; } = [];

    public CppUserType Type => typeDefinition;
    
    public Scope<ICppValue> NamespaceScope => namespaceScope;
    public Scope<ICppValue> InstanceParserScope => Type.CreateParserScope();
    public Scope<ICppType>  TypeScope => typeScope;
    

    public IEnumerable<CppUserType.MemberValue> AddedVariables => builder.Variables;
    
    public IEnumerable<ICppFunction> AddedConstructors => builder.Constructors;


    public void AddVariable(string name, ICppType type, InterpreterExpressionResult? initializer, MemberVisibility visibility)
    {
        builder.AddVariable(name, type, initializer, visibility);
    }

    public void AddFunction(string name, ICppFunction function, MemberVisibility visibility)
    {
        builder.AddFunction(name, function, visibility);
    }
    
    /// <summary>
    /// Register a new type constructor 
    /// </summary>
    /// <param name="constructor">A function that sets up a new instance, initializes members and calls the user defined constructor</param>
    /// <param name="userFunction"></param>
    /// <param name="visibility"></param>
    public void AddConstructor(ICppFunction constructor, CppUserFunction? userFunction, MemberVisibility visibility)
    {
        if (userFunction is not null)
            FunctionsToInitialize.Add(userFunction);
                
        builder.AddConstructor(constructor);
        if (visibility == MemberVisibility.Public)
            namespaceScope.BindFunction(constructor, typeDefinition.Name);
    }
}

public static class Stage2UserTypeParser
{
    public static Stage2CompoundTypeDefinition ParseCompoundTypeDefinition(
        Stage1CompoundTypeDefinition typeDef, 
        Scope<ICppValue> namespaceScope, 
        Scope<ICppType> typeScope)
    {
        List<(CppUserFunction Func, Scope<ICppValue> Closure)> functionsToInitialize = [];    
        
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
        bool hasAssignOperator = false;
        foreach (var function in functions)
        {
            throw new NotImplementedException("member functions");
        }
            
        if (!hasAssignOperator)
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

        if (userConstructors.Count == 0)
        {
            ParseConstructor(
                builder,
                null,
                MemberVisibility.Public,
                membersToInitialize!
            );
        }

        if (!HasCopyConstructor(builder.AddedConstructors, builder.Type))
        {
            builder.AddConstructor(new DefaultCopyConstructor(builder.Type), null,  MemberVisibility.Public);
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

        builderContext.AddConstructor(constructor, userFunction?.Function, visibility);
    }
    
    public static bool HasCopyConstructor(IEnumerable<ICppFunction> functions, ICppType instanceType) => 
        functions.Any(x => x.ParameterTypes is [{ IsReference: true } p] && p.Type.Equals(instanceType));

    public static MemberVisibility ToMemberVisibility(this AstVisibility visibility) => visibility switch
    {
        AstVisibility.Public => MemberVisibility.Public,
        AstVisibility.Private => MemberVisibility.Private,
        AstVisibility.Protected => MemberVisibility.Protected,
        _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
    };
}