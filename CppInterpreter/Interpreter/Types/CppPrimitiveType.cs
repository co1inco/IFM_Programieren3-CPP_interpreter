using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;


public abstract class CppPrimitiveType : ICppType
{
    private readonly ICppMemberInfo[] _members;
    
    protected CppPrimitiveType(string name, ICppFunction[]? functions = null)
    {
        Name = name;
        if (functions is not null)
            Functions = functions;
        
        _members = functions?
            .GroupBy(x => x.Name)
            .Select(ICppMemberInfo (x) => new CppMemberFunctionInfo(x.Key, x.ToArray()))
            .ToArray() ?? [];
        // typeof(int).GetMethod("").

        // MethodInfo mi = null!;
        // mi.In
    }
    
    public string Name { get; }

    public ICppConstructor[] Constructor { get; init; } = [];
    public ICppFunction[] Functions { get; } = [];
    public ICppConverter[] Converter { get; init; } = [];
    
    public bool IsAssignableTo(ICppType other) => other.GetType() == GetType();
    
    public bool Equals(ICppType? other) => Name == other?.Name;

    public abstract ICppValue Create();

    // TODO: Implement accessibility
    public IEnumerable<ICppMemberInfo> GetMembers(CppMemberBindingFlags flags) => _members;

    public IEnumerable<CppMemberFunctionInfo> GetFunctions(CppMemberBindingFlags flags)
    {
        foreach (var function in Functions.GroupBy(x => x.Name))
        {
            yield return new CppMemberFunctionInfo(function.Key, function.ToArray());
        }
    }
}