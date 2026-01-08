using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public class CppCallableType : ICppType
{
    public string Name => "Callable";

    public bool Equals(ICppType? other) => Name == other?.Name;

    public CppCallableType() { }

    public CppCallableType(ICppFunction[] functions)
    {
        CallableFunctions = functions;
    }

    public ICppFunction[] CallableFunctions { get; } = [];
    
    public ICppConstructor[] Constructor { get; } = [];
    public ICppFunction[] Functions { get; } = [];
    public ICppConverter[] Converter { get; }= [];
    
    public bool IsAssignableTo(ICppType other)
    {
        throw new NotSupportedException();
    }

    public ICppValue Create()
    {
        throw new NotSupportedException();
    }

    public IEnumerable<ICppMemberInfo> GetMembers(CppMemberBindingFlags flags) => [];
    public IEnumerable<CppMemberFunctionInfo> GetFunctions(CppMemberBindingFlags flags) => [];
}