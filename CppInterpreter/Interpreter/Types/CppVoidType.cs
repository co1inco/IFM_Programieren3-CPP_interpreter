using CppInterpreter.Interpreter.Functions;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public sealed class CppVoidType : CppPrimitiveType
{
    public static CppVoidType Instance { get; }= new CppVoidType();
    
    private CppVoidType() : base("void")
    {
        Constructor = [ new ConstructorFunction<CppVoidValue>(() => new CppVoidValue() ) ];
    }

    public override ICppValue Create() => new CppVoidValue();
};