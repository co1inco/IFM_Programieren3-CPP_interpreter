using CppInterpreter.Ast;

namespace CppInterpreter.Interpreter;

public class InterpreterException : Exception
{
    private static string CreateMessage(string message, AstMetadata metadata) =>
        $":{metadata.Source.Line}: '{metadata.Source.Text}' {message}";
    
    public InterpreterException(string message, AstMetadata metadata) : base(CreateMessage(message, metadata))
    {
        Metadata = metadata;
        BaseMessage = message;
    }

    public InterpreterException(string message, Exception innerException, AstMetadata metadata) : base(CreateMessage(message, metadata), innerException)
    {
        BaseMessage = message;
        Metadata = metadata;
    }
    
    
    public AstMetadata? Metadata { get; }
    
    public string BaseMessage { get; }
}