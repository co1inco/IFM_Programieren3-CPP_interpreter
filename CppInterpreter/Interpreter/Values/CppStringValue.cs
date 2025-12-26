using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public class CppStringValue(string initialValue) : ICppValue
{
    public static ICppType SType => CppTypes.String;

    public ICppType Type => SType;

    public string Value { get; set; } = initialValue;

    public string StringRep() => $"\"{Value}\"";
    public bool ToBool() => !string.IsNullOrEmpty(Value) && Value != "\0"; 

    public override string ToString() => StringRep();
}