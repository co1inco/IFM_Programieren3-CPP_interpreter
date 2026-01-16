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

public static class Stage2UserTypeParser
{
    public static Stage2CompoundTypeDefinition ParseCompoundTypeDefinition(Stage1CompoundTypeDefinition typeDef, Scope<ICppValue> scope, Scope<ICppType> typeScope)
    {
        List<(CppUserFunction Func, Scope<ICppValue> Closure)> userFunctions = [];    
        
        typeDef.Type.BuildMembers(scope, typeScope, (b, def, valueScope, typeScope) =>
        {
            // TODO: check for duplicate definitions
            
            foreach (var variable in def.Variables)
            {
                ParseMemberVariable(variable, typeScope, valueScope, b.AddVariable);
            }

            bool hasAssignOperator = false;
            foreach (var function in def.Functions)
            {
                throw new NotImplementedException("member functions");
            }

            if (!hasAssignOperator)
            {
                var f = new DefaultAssignmentOperator(typeDef.Type);
                b.AddFunction(f.Name, f, MemberVisibility.Public);
            }
            
            
            bool hasCopyConstructor = false;
            foreach (var constructor in def.Constructors)
            {
                
                throw new NotImplementedException("User defined constructors");
            }

            if (def.Constructors.Length == 0)
            {

                var membersToInitialize = b.Variables
                    .Select(x => (
                        x.MemberInfo.Name,
                        x.Initializer))
                    .Where(x => x.Initializer is not null);
                
                var constructorFunction = new BaseUserTypeConstructor(
                    typeDef.Type,
                    valueScope,
                    membersToInitialize!,
                    null
                );
                
                b.AddConstructor(constructorFunction);
                scope.BindFunction(constructorFunction, typeDef.Type.Name); // default constructor always public
            }

            if (!hasCopyConstructor)
            {
                var constructorFunction = new DefaultCopyConstructor(typeDef.Type);
                b.AddConstructor(constructorFunction);
                scope.BindFunction(constructorFunction, typeDef.Type.Name);
            }
            
            
            
            // TODO: register constructors as value scope functions
            
            // TODO: other members
        });

        return new Stage2CompoundTypeDefinition(typeDef.Type, userFunctions.ToArray());
    }

    public static void ParseMemberVariable(
        AstCompoundTypeMember<AstVarDefinition> varDefinition, 
        Scope<ICppType> typeScope,
        Scope<ICppValue> valueScope,
        Action<string, ICppType, InterpreterExpressionResult?, MemberVisibility> builder
        )
    {
        if (!typeScope.TryGetSymbol(varDefinition.Member.Type.Ident, out var type))
            throw varDefinition.Member.Type.CreateException("Unknown type");

        var visibility = varDefinition.Visibility switch
        {
            AstVisibility.Public => MemberVisibility.Public,
            AstVisibility.Private => MemberVisibility.Private,  
            AstVisibility.Protected => MemberVisibility.Protected,
            _ => throw new ArgumentOutOfRangeException()
        };

        //Note: usage of s3 parser here. Could be removed by only allowing const values
        //TODO: this should possibly only support const values (ie. atoms)
        var initializer = varDefinition.Member.Initializer is null
            ? null
            : Stage3ExpressionParser.ParseExpression(varDefinition.Member.Initializer, valueScope);
                
        builder(
            varDefinition.Member.Ident.Value,
            type,
            initializer,
            visibility
        );
    }

    public static void ParseConstructor(AstCompoundTypeMember<AstFuncDefinition> funcDefinition)
    {
        throw new NotImplementedException();
    }
    
}