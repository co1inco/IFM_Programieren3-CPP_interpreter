using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using CppInterpreter.Ast;

namespace CppInterpreter.CppParser;

public class ParserException(string message, AstMetadata metadata) 
    : Exception($":{metadata.Source.Line}:\n{message}\n{metadata.Source.Text} ")
{
    public string BaseMessage => message;
}

public class TypeNotFoundException(AstTypeIdentifier type) : ParserException($"Type '{type.Ident}' does not exist", type.Metadata);

public static class ParserExceptionExtension 
{
    [Pure]
    public static ParserException CreateException(this IAstNode node, string message) => 
        new ParserException(message, node.Metadata);
    
    [DoesNotReturn]
    public static void Throw(this IAstNode node, string message) => 
        throw CreateException(node, message);
    
    [DoesNotReturn]
    public static void ThrowNotFound(this AstTypeIdentifier type) => 
        throw new TypeNotFoundException(type);
}