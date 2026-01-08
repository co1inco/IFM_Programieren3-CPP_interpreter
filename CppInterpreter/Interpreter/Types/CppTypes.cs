using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter.Types;

public abstract class CppTypes
{
    public static ICppType Char => CppCharType.Instance;
    public static ICppType Int32 => CppInt32Type.Instance;
    public static ICppType Int64 => CppInt64Type.Instance;
    
    public static ICppType Void => CppVoidType.Instance;
    public static ICppType Boolean => CppBoolType.Instance;
    public static ICppType String => CppStringType.Instance;
    
    public static ICppType Callable => field ??= new CppCallableType();
    
}













