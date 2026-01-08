using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public sealed class CppBoolType : CppPrimitiveType
{
    public static CppBoolType Instance { get; } = new CppBoolType();
    
    private static ICppFunction[] MemberFunctions() =>
    [
        ..CppCommonOperators.EquatorOperators<CppBoolValue, bool>(),
        CppCommonOperators.PrimitiveAssignment<CppBoolValue, bool>(),
        // new MemberFunction<CppBoolValue, CppBoolValue, CppBoolValue>("operator&&", 
        //     (a, b) => new CppBoolValue(a.Value && b.Value)),
        // new MemberFunction<CppBoolValue, CppBoolValue, CppBoolValue>("operator||", 
        //     (a, b) => new CppBoolValue(a.Value || b.Value))
        new MemberFunction<CppBoolValue, CppBoolValue>("operator!", a => new CppBoolValue(!a.Value))
    ];
    
    private CppBoolType() : base("bool", MemberFunctions())
    {
        
    }

    public override ICppValue Create() => new CppBoolValue(false);
}
