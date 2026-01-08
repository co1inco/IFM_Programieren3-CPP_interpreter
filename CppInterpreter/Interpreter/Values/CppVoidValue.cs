using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public struct CppVoidValue : ICppValueT
{
    public static ICppType TypeOf => CppTypes.Void;
    public ICppType GetCppType => TypeOf;

    public string StringRep() => "(void)";
    public bool ToBool() => false;
}