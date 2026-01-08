using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public sealed class CppVoidType : CppPrimitiveType
{
    public static CppVoidType Instance { get; }= new CppVoidType();
    
    private CppVoidType() : base("void")
    {
        Constructor = [ new ConstructorFunction<CppVoidValueT>(() => new CppVoidValueT() ) ];
    }

    public override ICppValue Create() => new CppVoidValueT();
};