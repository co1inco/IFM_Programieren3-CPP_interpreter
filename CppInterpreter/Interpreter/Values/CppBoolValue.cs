using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public sealed class CppBoolValue(bool value)
    : CppPrimitiveValue<bool, CppBoolValue>(value)
        , ICppPrimitiveValueT<bool, CppBoolValue>
{
    public static ICppType TypeOf => CppTypes.Boolean;
    public static CppBoolValue Create(bool value) => new CppBoolValue(value);
    public bool ToBool() => Value;
}
