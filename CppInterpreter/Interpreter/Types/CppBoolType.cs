using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public sealed class CppBoolType : CppPrimitiveType
{
    public static CppBoolType Instance { get; } = new CppBoolType();
    
    private static ICppFunction[] MemberFunctions() =>
    [
        ..CppCommonOperators.EquatorOperators<CppBoolValueT, bool>(),
        CppCommonOperators.PrimitiveAssignment<CppBoolValueT, bool>(),
        // new MemberFunction<CppBoolValue, CppBoolValue, CppBoolValue>("operator&&", 
        //     (a, b) => new CppBoolValue(a.Value && b.Value)),
        // new MemberFunction<CppBoolValue, CppBoolValue, CppBoolValue>("operator||", 
        //     (a, b) => new CppBoolValue(a.Value || b.Value))
        new MemberFunction<CppBoolValueT, CppBoolValueT>("operator!", a => new CppBoolValueT(!a.Value))
    ];
    
    private CppBoolType() : base("bool", MemberFunctions())
    {
        
    }

    public override ICppValue Create() => new CppBoolValueT(false);
}
