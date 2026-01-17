using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public class CppStringValue(string initialValue) : ICppValueT
{
    public static ICppType TypeOf => CppTypes.String;

    public ICppType GetCppType => TypeOf;

    public string Value { get; set; } = initialValue;

    public string StringRep() => $"\"{Value}\"";
    public bool ToBool() => !string.IsNullOrEmpty(Value) && Value != "\0";
    public ICppValue Copy() => new CppStringValue(Value);
    public Scope<ICppValue> InstanceScope { get; } = new();

    public override string ToString() => StringRep();
}