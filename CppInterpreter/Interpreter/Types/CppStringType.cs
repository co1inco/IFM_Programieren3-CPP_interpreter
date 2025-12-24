using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public sealed class CppStringType : CppPrimitiveType
{
    public CppStringType() : base("string")
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
            new MemberFunction<CppStringValue, CppInt32Value>("legnth", a => new CppInt32Value(a.Value.Length)),
        ];

    }

    public override ICppValueBase Create() => new CppStringValue("");
}