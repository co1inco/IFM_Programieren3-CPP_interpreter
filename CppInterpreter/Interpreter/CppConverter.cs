using CppInterpreter.Interpreter.Types;
using CppInterpreter.Interpreter.Values;

namespace CppInterpreter.Interpreter;

public interface ICppConverter
{
    ICppType SourceType { get; }
    ICppType TargetType { get; }
    
    ICppValue Convert(ICppValue? value);
}

public class CppConverter<TSource, TTarget>(Func<TSource, TTarget> convert) : ICppConverter
    where TSource : ICppValue
    where TTarget : ICppValue
{
    public ICppType SourceType => TSource.TypeOf;
    public ICppType TargetType => TTarget.TypeOf;
    
    public ICppValue Convert(ICppValue? value)
    {
        if (value is not TSource source)
            throw new InvalidTypeException(TSource.TypeOf, value?.GetCppType);
        
        return convert(source);
    }
}