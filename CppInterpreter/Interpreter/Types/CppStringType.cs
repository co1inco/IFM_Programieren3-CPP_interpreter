using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public sealed class CppStringType : CppPrimitiveType
{
    public static CppStringType Instance { get; } = new CppStringType();
    
    private CppStringType() : base("string")
    {
        Functions =
        [
            new MemberFunction<CppStringValue, CppStringValue, CppStringValue>(
                "operator+", (a, b) => new CppStringValue(a.Value + b.Value)),
            // new MemberFunction<CppStringValue, CppStringValue, CppStringValue>(
            //     "operator=", (a, b) => { a.Value = b.Value; return b; })
            new MemberAction<CppStringValue, CppStringValue>(
                "operator=", (a, b) => { a.Value = b.Value; }),
            
            new MemberFunction<CppStringValue, CppInt32Value>("size", a => new CppInt32Value(a.Value.Length)),
            new MemberFunction<CppStringValue, CppInt32Value>("length", a => new CppInt32Value(a.Value.Length)),
        ];

        Constructor =
        [
            new ConstructorFunction<CppStringValue>(() => new CppStringValue("")),
            new ConstructorFunction<CppStringValue, CppStringValue>(s => new CppStringValue(s.Value))
        ];
    }

    public override ICppValueBase Create() => new CppStringValue("");
}
