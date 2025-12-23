using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public class CppCallableType : ICppType
{
    public string Name => "Callable";

    public bool Equals(ICppType? other) => Name == other?.Name;

    public ICppConstructor[] Constructor { get; } = [];
    public ICppFunction[] Functions { get; } = [];
    public ICppConverter[] Converter { get; }= [];
    
    public bool IsAssignableTo(ICppType other)
    {
        throw new NotSupportedException();
    }

    public ICppValueBase Create()
    {
        throw new NotSupportedException();
    }
}