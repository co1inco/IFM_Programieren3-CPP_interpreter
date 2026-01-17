using CppInterpreter.Interpreter.Types;

namespace CppInterpreter.Interpreter.Values;

public readonly struct CppVoidValue : ICppValueT
{
    public CppVoidValue() { }

    public static ICppType TypeOf => CppTypes.Void;
    public ICppType GetCppType => TypeOf;

    public string StringRep() => "(void)";
    public bool ToBool() => false;
    public ICppValue Copy() => this; // return self. is all the same anyways
    
    public Scope<ICppValue> InstanceScope { get; } = new();
}